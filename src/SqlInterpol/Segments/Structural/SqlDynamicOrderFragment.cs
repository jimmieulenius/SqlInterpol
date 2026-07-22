using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// A temporary placeholder that safely transports dynamic OrderBy requests 
/// until they can be resolved against the active query context.
/// </summary>
/// <param name="column">The dynamic column to sort by.</param>
/// <param name="direction">The sort direction, or <see langword="null"/> if omitted.</param>
public class SqlDynamicOrderFragment(SqlDynamicColumnFragment column, SqlOrderDirection? direction = null) : ISqlOrderFragment
{
    /// <summary>Gets the dynamic column to sort by.</summary>
    public SqlDynamicColumnFragment Column { get; } = column;

    /// <summary>Gets the sort direction, or <see langword="null"/> if omitted.</summary>
    public SqlOrderDirection? Direction { get; } = direction;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        throw new InvalidOperationException("SqlDynamicOrderFragment must be resolved by the Preprocessor before rendering.");
    }
}