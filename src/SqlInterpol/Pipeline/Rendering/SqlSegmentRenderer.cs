using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

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
                if (segment.Value is ISqlFragment projFragment)
                {
                    var mode = segment.RenderMode ?? ResolveRenderMode(context, index, segment, segments);
                    rendered = projFragment.ToSql(context, mode);
                }
                break;

            case SqlSegmentType.Reference:
                if (segment.Value is ISqlEntityBase entityCte && segment.Value is not ISqlQueryFragment && segment.Value is not ISqlQuery)
                {
                    bool isCte = false;
                    int lookahead = index + 1;
                    // 🌟 FIX: Allow Raw strings
                    while (lookahead < segments.Count && (segments[lookahead].Type == SqlSegmentType.Literal || segments[lookahead].Type == SqlSegmentType.Raw) && string.IsNullOrWhiteSpace(segments[lookahead].Value as string)) lookahead++;

                    if (lookahead < segments.Count && (segments[lookahead].Type == SqlSegmentType.Literal || segments[lookahead].Type == SqlSegmentType.Raw))
                    {
                        var text = segments[lookahead].Value?.ToString()?.TrimStart();
                        bool hasAs = text?.StartsWith($"{SqlKeyword.As.Value} ", StringComparison.OrdinalIgnoreCase) == true
                            || text?.StartsWith($"{SqlKeyword.As.Value}\n", StringComparison.OrdinalIgnoreCase) == true
                            || text?.StartsWith($"{SqlKeyword.As.Value}\r", StringComparison.OrdinalIgnoreCase) == true
                            || text?.StartsWith($"{SqlKeyword.As.Value}(", StringComparison.OrdinalIgnoreCase) == true
                            || string.Equals(text, SqlKeyword.As.Value, StringComparison.OrdinalIgnoreCase);

                        if (hasAs)
                        {
                            var afterAs = text!.Substring(2).TrimStart();
                            if (afterAs.StartsWith("(")) isCte = true;
                            else if (string.IsNullOrEmpty(afterAs))
                            {
                                int next = lookahead + 1;
                                while (next < segments.Count && (segments[next].Type == SqlSegmentType.Literal || segments[next].Type == SqlSegmentType.Raw) && string.IsNullOrWhiteSpace(segments[next].Value as string)) next++;
                                if (next < segments.Count)
                                {
                                    var nextSeg = segments[next];
                                    if ((nextSeg.Type == SqlSegmentType.Literal || nextSeg.Type == SqlSegmentType.Raw) && nextSeg.Value?.ToString()?.TrimStart().StartsWith("(") == true) isCte = true;
                                    else if (nextSeg.Value is ISqlQueryFragment || nextSeg.Value is ISqlQuery) isCte = true;
                                }
                            }
                        }
                    }

                    if (isCte)
                    {
                        var meta = SqlMetadataRegistry.GetMetadata(entityCte.ModelType);
                        rendered = context.Dialect.QuoteIdentifier(meta.Name);
                        break; 
                    }
                }
                
                if (segment.Value is ISqlFragment fragment)
                {
                    var mode = segment.RenderMode ?? ResolveRenderMode(context, index, segment, segments);
                    rendered = fragment.ToSql(context, mode);
                }
                break;

            case SqlSegmentType.Literal:
            {
                var text = segment.Value?.ToString() ?? string.Empty;
                
                if (index > 0 && segments[index - 1].Value is ISqlQueryFragment prevSubquery)
                {
                    var trimmed = text.TrimStart();
                    if (trimmed.StartsWith(")"))
                    {
                        var afterClose = trimmed[1..].TrimStart();
                        bool alreadyHasAs = afterClose.StartsWith($"{SqlKeyword.As.Value} ", StringComparison.OrdinalIgnoreCase)
                                         || afterClose.StartsWith($"{SqlKeyword.As.Value}\n", StringComparison.OrdinalIgnoreCase)
                                         || afterClose.StartsWith($"{SqlKeyword.As.Value}\r", StringComparison.OrdinalIgnoreCase);

                        bool requiresAlias = true;
                        if (index >= 2 && (segments[index - 2].Type == SqlSegmentType.Literal || segments[index - 2].Type == SqlSegmentType.Raw))
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

                        if (!alreadyHasAs)
                        {
                            int lookahead = index + 1;
                            while (lookahead < segments.Count && (segments[lookahead].Type == SqlSegmentType.Literal || segments[lookahead].Type == SqlSegmentType.Raw) && string.IsNullOrWhiteSpace(segments[lookahead].Value?.ToString()))
                            {
                                lookahead++;
                            }

                            if (lookahead < segments.Count)
                            {
                                var nextSeg = segments[lookahead];
                                var nextText = nextSeg.Value?.ToString()?.TrimStart();

                                if (nextText != null && (
                                    nextText.StartsWith($"{SqlKeyword.As.Value} ", StringComparison.OrdinalIgnoreCase) || 
                                    nextText.StartsWith($"{SqlKeyword.As.Value}\n", StringComparison.OrdinalIgnoreCase) || 
                                    nextText.StartsWith($"{SqlKeyword.As.Value}\r", StringComparison.OrdinalIgnoreCase) || 
                                    nextText.Equals(SqlKeyword.As.Value, StringComparison.OrdinalIgnoreCase)))
                                {
                                    alreadyHasAs = true;
                                }
                                else if (nextSeg.Type == SqlSegmentType.Raw)
                                {
                                    alreadyHasAs = true;
                                }
                            }
                        }

                        if (!alreadyHasAs && requiresAlias)
                        {
                            var asAlias = prevSubquery.ToSql(context, SqlRenderMode.AsAlias);
                            
                            if (string.IsNullOrWhiteSpace(asAlias))
                            {
                                return text;
                            }
                            
                            int wsLen = text.Length - trimmed.Length;
                            string ws = text[..wsLen];
                            if (ws.EndsWith(" ") || ws.EndsWith("\t")) ws = ws[..^1];
                            
                            return ws + ") " + asAlias + (trimmed[1..].StartsWith(" ") ? trimmed[1..] : " " + trimmed[1..].TrimStart());
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
                    var mode = segment.RenderMode ?? SqlRenderMode.Default;
                    rendered = rawFrag.ToSql(context, mode);
                }
                else
                {
                    rendered = segment.Value?.ToString() ?? string.Empty;
                }
                break;
        }

        if (segment.Value is ISqlFragment && segment.Type != SqlSegmentType.Projection && rendered != null)
        {
            if (!IsCollectionFragment(segment.Value.GetType()))
            {
                rendered = ApplyAutoIndentation(rendered, index, segments);
            }
        }

        return rendered;
    }

    private static bool IsCollectionFragment(Type? type)
    {
        if (type == typeof(SqlRawCollectionFragment)) return true;

        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SqlCollectionFragmentBase<>))
            {
                return true;
            }
            type = type.BaseType;
        }
        return false;
    }

    private string ApplyAutoIndentation(string rendered, int index, IReadOnlyList<SqlSegment> segments)
    {
        // 🌟 FIX: Allow Raw strings to trigger auto-indentation just like Literals!
        if (index <= 0 || (segments[index - 1].Type != SqlSegmentType.Literal && segments[index - 1].Type != SqlSegmentType.Raw))
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

    private SqlRenderMode ResolveRenderMode(ISqlContext context, int index, SqlSegment segment, IReadOnlyList<SqlSegment> segments)
    {
        if (segment.Value is SqlColumnReferenceBase)
        {
            // 🌟 FIX: Allow Raw strings
            if (index > 0 && (segments[index - 1].Type == SqlSegmentType.Literal || segments[index - 1].Type == SqlSegmentType.Raw))
            {
                var prevText = segments[index - 1].Value?.ToString()?.TrimEnd();
                if (prevText != null && prevText.EndsWith("AS", StringComparison.OrdinalIgnoreCase))
                {
                    if (prevText.Length == 2 || !char.IsLetterOrDigit(prevText[^3]))
                    {
                        return SqlRenderMode.AliasOnly;
                    }
                }
            }
            return SqlRenderMode.Default;
        }

        if (segment.Value is not ISqlEntityBase entity) return SqlRenderMode.Default;

        // 🌟 FIX: Allow Raw strings
        if (index > 0 && (segments[index - 1].Type == SqlSegmentType.Literal || segments[index - 1].Type == SqlSegmentType.Raw))
        {
            var prevText = segments[index - 1].Value?.ToString()?.TrimEnd();
            if (prevText != null && prevText.EndsWith("("))
            {
                return SqlRenderMode.Default;
            }
        }

        int lookahead = index + 1;
        // 🌟 FIX: Allow Raw strings
        while (lookahead < segments.Count && (segments[lookahead].Type == SqlSegmentType.Literal || segments[lookahead].Type == SqlSegmentType.Raw) && string.IsNullOrWhiteSpace(segments[lookahead].Value as string))
        {
            lookahead++;
        }

        // 🌟 FIX: Allow Raw strings
        if (lookahead < segments.Count && (segments[lookahead].Type == SqlSegmentType.Literal || segments[lookahead].Type == SqlSegmentType.Raw))
        {
            var text = segments[lookahead].Value?.ToString()?.TrimStart();
            
            bool hasAs = text?.StartsWith($"{SqlKeyword.As.Value} ", StringComparison.OrdinalIgnoreCase) == true
                || text?.StartsWith($"{SqlKeyword.As.Value}\n", StringComparison.OrdinalIgnoreCase) == true
                || string.Equals(text, SqlKeyword.As.Value, StringComparison.OrdinalIgnoreCase);

            if (hasAs)
            {
                return SqlRenderMode.BaseName;
            }

            if (entity.Reference is ISqlReference entRef && !string.IsNullOrEmpty(entRef.Alias) && text != null)
            {
                var unquotedAlias = context.Dialect.UnquoteIdentifier(entRef.Alias);
                var quotedAlias = context.Dialect.QuoteIdentifier(entRef.Alias);
                
                if (text.StartsWith(unquotedAlias, StringComparison.OrdinalIgnoreCase) || 
                    text.StartsWith(quotedAlias, StringComparison.OrdinalIgnoreCase))
                {
                    int matchLen = text.StartsWith(quotedAlias, StringComparison.OrdinalIgnoreCase) ? quotedAlias.Length : unquotedAlias.Length;
                    if (text.Length == matchLen || !char.IsLetterOrDigit(text[matchLen]))
                    {
                        return SqlRenderMode.BaseName;
                    }
                }
            }

            if (text?.StartsWith(")") == true)
            {
                return SqlRenderMode.Default;
            }
        }
        else if (lookahead < segments.Count && segments[lookahead].Type == SqlSegmentType.Raw)
        {
            return SqlRenderMode.BaseName;
        }

        if (entity.Reference is ISqlReference reference && !string.IsNullOrEmpty(reference.Alias))
        {
            return SqlRenderMode.Declaration;
        }

        return entity is ISqlQueryFragment ? SqlRenderMode.Default : SqlRenderMode.BaseName;
    }
}