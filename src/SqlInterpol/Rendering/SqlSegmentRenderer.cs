using SqlInterpol.Config;
using SqlInterpol.Metadata;

namespace SqlInterpol.Rendering;

public class SqlSegmentRenderer : ISqlSegmentRenderer
{
    public static readonly SqlSegmentRenderer Instance = new();

    public string? Render(SqlContext context, SqlSegment segment, int index, IReadOnlyList<SqlSegment> segments)
    {
        string? rendered = null;

        switch (segment.Type)
        {
            case SqlSegmentType.Projection:
            case SqlSegmentType.Reference:
                if (segment.Value is ISqlFragment fragment)
                {
                    // Prioritize the segment's specific RenderMode if set by the parser
                    var mode = segment.RenderMode ?? ResolveRenderMode(index, segment, segments);

                    // NEW: CTE Schema Override Check
                    // We check if the current context provides parser state, and if so, if this is a CTE
                    if (segment.Type == SqlSegmentType.Reference && 
                        segment.Value is ISqlEntityBase entity &&
                        context is Parsing.ISqlParserContext parserContext &&
                        parserContext.ParserState.EntityRoles.TryGetValue(entity, out var role) &&
                        role == Parsing.SqlEntityRole.Cte)
                    {
                        // CTEs don't have schemas! We build it raw.
                        if (mode == SqlRenderMode.BaseName || mode == SqlRenderMode.Declaration)
                        {
                            string baseName = context.Dialect.QuoteIdentifier(entity.Reference.FallbackAlias);

                            if (mode == SqlRenderMode.Declaration && entity.Reference is ISqlReference entRef && !string.IsNullOrEmpty(entRef.Alias))
                            {
                                rendered = $"{baseName} AS {context.Dialect.QuoteIdentifier(entRef.Alias)}";
                            }
                            else
                            {
                                rendered = baseName;
                            }
                        }
                        else 
                        {
                            // Fallback for modes like AliasOnly
                            rendered = fragment.ToSql(context, mode);
                        }
                    }
                    else
                    {
                        // Standard entity/projection rendering
                        rendered = fragment.ToSql(context, mode);
                    }
                }
                break;

            case SqlSegmentType.Literal:
            {
                var text = segment.Value?.ToString() ?? string.Empty;
                
                if (index > 0 && segments[index - 1].Value is ISqlQuery prevSubquery)
                {
                    var trimmed = text.TrimStart();
                    if (trimmed.StartsWith(")"))
                    {
                        var afterClose = trimmed[1..].TrimStart();
                        bool alreadyHasAs = afterClose.StartsWith($"{SqlKeyword.As.Value} ", StringComparison.OrdinalIgnoreCase)
                                        || afterClose.StartsWith($"{SqlKeyword.As.Value}\n", StringComparison.OrdinalIgnoreCase)
                                        || afterClose.StartsWith($"{SqlKeyword.As.Value}\r", StringComparison.OrdinalIgnoreCase);

                        bool requiresAlias = true;
                        if (index >= 2 && segments[index - 2].Type == SqlSegmentType.Literal)
                        {
                            var beforeSubquery = segments[index - 2].Value?.ToString()?.TrimEnd();

                            if (beforeSubquery != null && beforeSubquery.EndsWith("("))
                            {
                                var textBeforeParen = beforeSubquery[..^1].TrimEnd();
                                
                                if (context.Dialect.IsExpressionContext(textBeforeParen))
                                {
                                    requiresAlias = false;
                                }
                            }
                        }

                        if (!alreadyHasAs && requiresAlias)
                        {
                            int ws = text.Length - trimmed.Length;
                            var asAlias = prevSubquery.ToSql(context, SqlRenderMode.AsAlias);
                            return text[..ws] + ") " + asAlias + trimmed[1..];
                        }
                    }
                }

                return text;
            }

            case SqlSegmentType.Parameter:
                return segment.Value?.ToString() ?? string.Empty;

            case SqlSegmentType.Raw:
                if (segment.Value is ISqlFragment rawFrag)
                {
                    rendered = rawFrag.ToSql(context, SqlRenderMode.Default);
                }
                else
                {
                    rendered = segment.Value?.ToString() ?? string.Empty;
                }
                break;
        }

        if (segment.Value is ISqlQuery && segment.Type != SqlSegmentType.Projection && rendered != null)
        {
            rendered = ApplyAutoIndentation(rendered, index, segments);
        }

        return rendered;
    }

    private string ApplyAutoIndentation(string rendered, int index, IReadOnlyList<SqlSegment> segments)
    {
        if (index <= 0 || segments[index - 1].Type != SqlSegmentType.Literal)
        {
            return rendered;
        }

        var prevLiteral = segments[index - 1].Value?.ToString();
        if (string.IsNullOrEmpty(prevLiteral)) return rendered;

        int lastNewline = prevLiteral.LastIndexOf('\n');
        if (lastNewline < 0) return rendered;

        // Capture the exact whitespace following the last newline in the previous literal
        var indentChars = prevLiteral.Substring(lastNewline + 1)
            .TakeWhile(c => c == ' ' || c == '\t')
            .ToArray();

        if (indentChars.Length > 0)
        {
            var indent = new string(indentChars);
            
            // Standardize newlines and inject the parent's indentation level
            // We use \n + indent because the sub-builder uses \n for its internal lines
            return rendered.Replace("\n", "\n" + indent);
        }

        return rendered;
    }

    private SqlRenderMode ResolveRenderMode(int index, SqlSegment segment, IReadOnlyList<SqlSegment> segments)
    {
        // if (segment.RenderMode == SqlRenderMode.AliasOnly) return SqlRenderMode.AliasOnly;

        if (segment.Value is not ISqlEntity entity) return SqlRenderMode.Default;

        // 1. WYSIWYG Look-Behind: Did the user manually open a parenthesis?
        // E.g., WHERE IN ( {{subquery}} ) or FROM ( {{subquery}} ) AS stats
        if (index > 0 && segments[index - 1].Type == SqlSegmentType.Literal)
        {
            var prevText = segments[index - 1].Value?.ToString()?.TrimEnd();
            if (prevText != null && prevText.EndsWith("("))
            {
                // The user controls the wrapping. Just return the raw SQL body.
                return SqlRenderMode.Default;
            }
        }

        // 2. Look-Ahead Checks
        if (index + 1 < segments.Count)
        {
            var next = segments[index + 1];
            if (next.Type == SqlSegmentType.Literal)
            {
                var text = next.Value?.ToString()?.TrimStart();
                
                // If they manually type AS, drop the declaration but keep parentheses
                if (text?.StartsWith($"{SqlKeyword.As} ", StringComparison.OrdinalIgnoreCase) == true
                    || text?.StartsWith($"{SqlKeyword.As}\n", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return SqlRenderMode.BaseName;
                }

                // WYSIWYG Look-Ahead: If the next string closes a parenthesis, they opened one.
                if (text?.StartsWith(")") == true)
                {
                    return SqlRenderMode.Default;
                }
            }
            else if (next.Type == SqlSegmentType.Raw)
            {
                return SqlRenderMode.BaseName;
            }
        }

        // 3. Auto-wrapping for un-wrapped physical tables / queries
        return !string.IsNullOrEmpty(entity.Reference.Alias)
            ? SqlRenderMode.Declaration
            : SqlRenderMode.BaseName;
    }
}