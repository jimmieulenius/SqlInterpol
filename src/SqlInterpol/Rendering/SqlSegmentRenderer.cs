using SqlInterpol.Config;

namespace SqlInterpol.Rendering;

public class SqlSegmentRenderer : ISqlSegmentRenderer
{
    public static readonly SqlSegmentRenderer Instance = new();

    public string? Render(SqlContext context, SqlSegment segment, int index, IReadOnlyList<SqlSegment> segments)
    {
        switch (segment.Type)
        {
            case SqlSegmentType.Projection:
            case SqlSegmentType.Reference:
                if (segment.Value is ISqlFragment fragment)
                {
                    var mode = ResolveRenderMode(index, segment, segments);

                    return fragment.ToSql(context, mode);
                }
                break;

            case SqlSegmentType.Literal:
            case SqlSegmentType.Parameter:
                return segment.Value?.ToString() ?? string.Empty;

            case SqlSegmentType.Raw:
                if (segment.Value is ISqlFragment rawFrag)
                {
                    var rendered = rawFrag.ToSql(context, SqlRenderMode.Default);

                    // AUTO-INDENTATION: Align subqueries perfectly with their parent query
                    if (rawFrag is ISqlQuery && index > 0 && segments[index - 1].Type == SqlSegmentType.Literal)
                    {
                        var prevLiteral = segments[index - 1].Value?.ToString();

                        if (prevLiteral != null)
                        {
                            int lastNewline = prevLiteral.LastIndexOf('\n');

                            if (lastNewline >= 0)
                            {
                                // Extract only the spaces/tabs immediately following the last newline
                                var indentChars = prevLiteral.Substring(lastNewline + 1)
                                    .TakeWhile(c => c == ' ' || c == '\t')
                                    .ToArray();
                                
                                if (indentChars.Length > 0)
                                {
                                    var indent = new string(indentChars);
                                    
                                    // Shift all internal newlines by the parent's indentation
                                    rendered = rendered.Replace("\n", "\n" + indent);
                                }
                            }
                        }
                    }

                    return rendered;
                }
                else
                {
                    return segment.Value?.ToString() ?? string.Empty;
                }
        }

        return null;
    }

    private SqlRenderMode ResolveRenderMode(int index, SqlSegment segment, IReadOnlyList<SqlSegment> segments)
    {
        if (segment.IsAliasTarget)
        {
            return SqlRenderMode.AliasOnly;
        }

        if (segment.Value is not ISqlEntity entity)
        {
            return SqlRenderMode.Default;
        }

        if (index + 1 < segments.Count)
        {
            var next = segments[index + 1];

            if (next.Type == SqlSegmentType.Literal)
            {
                var text = next.Value?.ToString()?.TrimStart();

                if (text?.StartsWith("AS ", StringComparison.OrdinalIgnoreCase) == true
                    || text?.StartsWith("AS\n", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return SqlRenderMode.BaseName;
                }
            }
            else if (next.Type == SqlSegmentType.Raw)
            {
                return SqlRenderMode.BaseName;
            }
        }

        return !string.IsNullOrEmpty(entity.Reference.Alias)
            ? SqlRenderMode.Declaration
            : SqlRenderMode.BaseName;
    }
}