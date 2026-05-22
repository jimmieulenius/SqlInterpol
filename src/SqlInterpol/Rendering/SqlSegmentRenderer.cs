using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// The default implementation of <see cref="ISqlSegmentRenderer"/> that converts individual
/// <see cref="SqlSegment"/> instances to their rendered SQL strings, handling entity promotion,
/// subquery alias injection, and render-mode resolution.
/// </summary>
public class SqlSegmentRenderer : ISqlSegmentRenderer
{
    public static readonly SqlSegmentRenderer Instance = new();

    /// <inheritdoc />
    public string? Render(ISqlContext context, SqlSegment segment, int index, IReadOnlyList<SqlSegment> segments)
    {
        string? rendered = null;

        switch (segment.Type)
        {
            case SqlSegmentType.Projection:
            case SqlSegmentType.Reference:
                if (segment.Value is ISqlFragment fragment)
                {
                    var mode = segment.RenderMode ?? ResolveRenderMode(index, segment, segments);

                    if (segment.Type == SqlSegmentType.Reference && 
                        segment.Value is ISqlEntityBase entity &&
                        context is ISqlParserContext parserContext &&
                        parserContext.ParserState.EntityRoles.TryGetValue(entity, out var role) &&
                        role == SqlEntityRole.Cte)
                    {
                        if (mode == SqlRenderMode.BaseName || mode == SqlRenderMode.Declaration)
                        {
                            string baseName = context.Dialect.QuoteIdentifier(SqlMetadataRegistry.GetEntityName(entity));

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
                            rendered = fragment.ToSql(context, mode);
                        }
                    }
                    else
                    {
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
                                
                                if (context.Dialect.IsExpressionContext(textBeforeParen) ||
                                    textBeforeParen.EndsWith("AS", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>Indents all newlines in <paramref name="rendered"/> to match the whitespace prefix of the preceding literal segment.</summary>
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

        var indentChars = prevLiteral[(lastNewline + 1)..]
            .TakeWhile(c => c == ' ' || c == '\t')
            .ToArray();

        if (indentChars.Length > 0)
        {
            var indent = new string(indentChars);

            return rendered.Replace("\n", $"\n{indent}");
        }

        return rendered;
    }

    /// <summary>Determines the appropriate <see cref="SqlRenderMode"/> for the given entity segment based on context look-behind and look-ahead.</summary>
    private SqlRenderMode ResolveRenderMode(int index, SqlSegment segment, IReadOnlyList<SqlSegment> segments)
    {
        if (segment.Value is not ISqlEntityBase entity) return SqlRenderMode.Default;

        // 1. Look-Behind: Did the user manually open a parenthesis?
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
                
                // If they manually type AS, drop the declaration but keep parens if it's a query
                if (text?.StartsWith($"{SqlKeyword.As.Value} ", StringComparison.OrdinalIgnoreCase) == true
                    || text?.StartsWith($"{SqlKeyword.As.Value}\n", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return SqlRenderMode.BaseName;
                }

                // If they manually type the alias directly (e.g., " p1"), suppress the auto-generated alias
                if (entity.Reference is ISqlReference entRef && !string.IsNullOrEmpty(entRef.Alias) && text != null &&
                    text.StartsWith(entRef.Alias, StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure it's a whole word match (so "p1" doesn't falsely match "p11")
                    if (text.Length == entRef.Alias.Length || !char.IsLetterOrDigit(text[entRef.Alias.Length]))
                    {
                        return SqlRenderMode.BaseName;
                    }
                }

                // If the next string closes a parenthesis, they opened one.
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
        if (entity.Reference is ISqlReference reference && !string.IsNullOrEmpty(reference.Alias))
        {
            return SqlRenderMode.Declaration;
        }

        // Subqueries without an alias (like UNION) render natively to avoid arbitrary parentheses.
        // Physical tables without an alias render their BaseName (e.g., [Products]).
        return entity is ISqlQuery ? SqlRenderMode.Default : SqlRenderMode.BaseName;
    }
}