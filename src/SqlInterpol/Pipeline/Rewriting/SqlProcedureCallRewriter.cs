using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// Transpiles ANSI-standard <c>CALL procedure_name(args)</c> syntax to the target dialect's
/// native stored procedure invocation form.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Dialect</term><description>Transformation</description></listheader>
/// <item><term>PostgreSQL / MySQL / SQLite</term><description>Pass-through — native CALL support, zero allocations.</description></item>
/// <item><term>SQL Server</term><description><c>CALL proc(a, b)</c> → <c>EXEC proc a, b</c></description></item>
/// <item><term>Oracle</term><description><c>CALL proc(a, b)</c> → <c>BEGIN CALL proc(a, b); END;</c></description></item>
/// </list>
/// </remarks>
public class SqlProcedureCallRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlPipelineState state) => state.HasTag(SqlSegmentTag.CallKeyword);

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var dialect = context.Dialect.Kind;

        // PostgreSQL, MySQL, SQLite natively support CALL — zero allocations.
        if (dialect == SqlDialectKind.PostgreSql ||
            dialect == SqlDialectKind.MySql ||
            dialect == SqlDialectKind.SqLite)
        {
            return segments;
        }

        int callIdx = FindCallIndex(segments);
        if (callIdx < 0) return segments;

        if (dialect == SqlDialectKind.SqlServer)
            return RewriteForSqlServer(segments, callIdx);

        if (dialect == SqlDialectKind.Oracle)
            return RewriteForOracle(segments, callIdx);

        return segments;
    }

    private static int FindCallIndex(IReadOnlyList<SqlSegment> segments)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].HasTag(SqlSegmentTag.CallKeyword)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Finds the ( and ) positions that delimit the argument list beginning after <paramref name="startIdx"/>.
    /// </summary>
    private static bool TryFindArgumentBounds(
        IReadOnlyList<SqlSegment> segments, int startIdx,
        out int openSegIdx, out int openCharIdx,
        out int closeSegIdx, out int closeCharIdx)
    {
        openSegIdx = openCharIdx = closeSegIdx = closeCharIdx = -1;
        int depth = 0;

        for (int i = startIdx; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.Type != SqlSegmentType.Literal || seg.Value is not string text) continue;

            for (int j = 0; j < text.Length; j++)
            {
                char c = text[j];
                if (c == '(')
                {
                    if (openSegIdx < 0) { openSegIdx = i; openCharIdx = j; }
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0 && openSegIdx >= 0) { closeSegIdx = i; closeCharIdx = j; return true; }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Transforms <c>CALL proc(a, b)</c> → <c>EXEC proc a, b</c> for SQL Server.
    /// </summary>
    private static IReadOnlyList<SqlSegment> RewriteForSqlServer(IReadOnlyList<SqlSegment> segments, int callIdx)
    {
        if (!TryFindArgumentBounds(segments, callIdx + 1, out int openSegIdx, out int openCharIdx, out int closeSegIdx, out int closeCharIdx))
            return segments;

        var result = new List<SqlSegment>(segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            if (i == callIdx)
            {
                result.Add(new SqlSegment(SqlSegmentType.Literal, "EXEC", null, null));
                continue;
            }

            var seg = segments[i];

            if (i == openSegIdx && i == closeSegIdx)
            {
                // Both delimiters are in the same literal segment (e.g. no-argument call: "proc()")
                var text = (string)seg.Value!;
                var sb = new System.Text.StringBuilder(text);
                sb.Remove(closeCharIdx, 1);
                // Replace ( with a space so arguments remain separated from the procedure name
                sb[openCharIdx] = ' ';
                var newText = sb.ToString().TrimEnd();
                if (!string.IsNullOrEmpty(newText))
                    result.Add(new SqlSegment(SqlSegmentType.Literal, newText, seg.RenderMode, seg.Tags));
                continue;
            }

            if (i == openSegIdx)
            {
                var text = (string)seg.Value!;
                // Replace ( with a space so the first argument is separated from the procedure name
                var newText = text.Remove(openCharIdx, 1).Insert(openCharIdx, " ");
                if (!string.IsNullOrWhiteSpace(newText))
                    result.Add(new SqlSegment(SqlSegmentType.Literal, newText, seg.RenderMode, seg.Tags));
                continue;
            }

            if (i == closeSegIdx)
            {
                var text = (string)seg.Value!;
                var newText = text.Remove(closeCharIdx, 1);
                if (!string.IsNullOrEmpty(newText))
                    result.Add(new SqlSegment(SqlSegmentType.Literal, newText, seg.RenderMode, seg.Tags));
                continue;
            }

            result.Add(seg);
        }

        return result;
    }

    /// <summary>
    /// Wraps <c>CALL proc(a, b)</c> in a PL/SQL block: <c>BEGIN CALL proc(a, b); END;</c> for Oracle.
    /// </summary>
    private static IReadOnlyList<SqlSegment> RewriteForOracle(IReadOnlyList<SqlSegment> segments, int callIdx)
    {
        if (!TryFindArgumentBounds(segments, callIdx + 1, out _, out _, out int closeSegIdx, out _))
            return segments;

        var result = new List<SqlSegment>(segments.Count + 2);

        result.Add(new SqlSegment(SqlSegmentType.Literal, "BEGIN ", null, null));

        for (int i = 0; i < segments.Count; i++)
        {
            result.Add(segments[i]);

            if (i == closeSegIdx)
                result.Add(new SqlSegment(SqlSegmentType.Literal, "; END;", null, null));
        }

        return result;
    }
}
