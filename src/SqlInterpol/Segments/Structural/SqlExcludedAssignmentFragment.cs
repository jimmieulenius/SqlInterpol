using SqlInterpol.Configuration;
using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// An <see cref="ISqlAssignmentFragment"/> for UPSERT operations that means
/// "update this column to the value that was just inserted".
/// </summary>
/// <remarks>
/// <para>
/// Renders as <c>col = EXCLUDED.col</c> — the PostgreSQL/SQLite canonical form — when
/// passed through the pipeline unchanged.
/// </para>
/// <para>
/// The dialect-specific rewriters transform this into their own equivalents:
/// <list type="bullet">
///   <item><description>SQL Server (<c>MERGE</c>): <c>target.col = source.col</c></description></item>
///   <item><description>MySQL: <c>col = VALUES(col)</c></description></item>
/// </list>
/// </para>
/// <para>
/// This fragment carries no parameter value; all parameter holes live in the INSERT VALUES
/// part of the upsert statement, not in the DO UPDATE SET clause.
/// </para>
/// </remarks>
public sealed class SqlExcludedAssignmentFragment(ISqlReference reference) : ISqlAssignmentFragment
{
    /// <inheritdoc />
    public ISqlReference Reference { get; } = reference;

    /// <summary>
    /// Always <see langword="null"/>: excluded-reference assignments carry no separate
    /// parameter value — the update reuses the value from the INSERT clause.
    /// </summary>
    public object? Value => null;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var quotedColumn = Reference.ToSql(context, SqlRenderMode.BaseName);
        return $"{quotedColumn} = EXCLUDED.{quotedColumn}";
    }
}
