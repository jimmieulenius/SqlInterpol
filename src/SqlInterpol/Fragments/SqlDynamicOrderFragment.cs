namespace SqlInterpol;

/// <summary>
/// A temporary placeholder that safely transports dynamic OrderBy requests 
/// until they can be resolved against the active AST context.
/// </summary>
public class SqlDynamicOrderFragment : ISqlOrderFragment, ISqlFragment
{
    public SqlDynamicColumnFragment Column { get; }
    public SqlOrderDirection? Direction { get; } // Nullable to track omitted directions

    public SqlDynamicOrderFragment(SqlDynamicColumnFragment column, SqlOrderDirection? direction = null)
    {
        Column = column;
        Direction = direction;
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        throw new InvalidOperationException("SqlDynamicOrderFragment must be resolved by the Preprocessor before rendering.");
    }
}