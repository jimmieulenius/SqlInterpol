using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects.MySql;

/// <summary>
/// A helper fragment that strips the leading <c>SET</c> keyword from a <see cref="SqlSetFragment"/>
/// for use in MySQL's ON DUPLICATE KEY UPDATE clause.
/// When the set fragment contains <see cref="SqlExcludedAssignmentFragment"/> entries (as produced
/// by <c>AppendUpsert</c>), each is rendered as <c>col = VALUES(col)</c> to reference the inserted
/// value instead of a new parameter.
/// </summary>
public class MySqlUpdateFragment : ISqlFragment
{
    private readonly SqlSetFragment _original;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlUpdateFragment"/> class.
    /// </summary>
    /// <param name="original">The SET fragment to wrap.</param>
    public MySqlUpdateFragment(SqlSetFragment original)
    {
        _original = original;
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // When the set fragment contains CRUD-upsert excluded-reference assignments, transform
        // each EXCLUDED.col to VALUES(col) — the MySQL equivalent of "use the inserted value".
        if (_original.Assignments.Any(a => a is SqlExcludedAssignmentFragment))
        {
            var parts = _original.Assignments.Select(a =>
            {
                var colName = a.Reference.ToSql(context, SqlRenderMode.BaseName);
                string valueExpr = a is SqlExcludedAssignmentFragment
                    ? $"VALUES({colName})"
                    : a.ToSql(context).Split('=').Last().Trim();
                return $"{colName} = {valueExpr}";
            });
            return " " + string.Join(", ", parts);
        }

        // Standard user-written SQL: just strip the SET keyword.
        var sql = _original.ToSql(context, mode);
        if (sql.StartsWith("SET", StringComparison.OrdinalIgnoreCase)) return " " + sql[3..].TrimStart();
        return " " + sql;
    }
}