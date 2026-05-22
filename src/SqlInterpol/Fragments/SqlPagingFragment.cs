
namespace SqlInterpol;

/// <summary>
/// Represents a paging clause (LIMIT / OFFSET), delegating dialect-specific
/// rendering to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
public class SqlPagingFragment : ISqlFragment
{
    /// <summary>Gets the maximum number of rows to return.</summary>
    public int Limit { get; }

    /// <summary>Gets the number of rows to skip before returning results.</summary>
    public int Offset { get; }

    /// <summary>
    /// Initializes a new <see cref="SqlPagingFragment"/>.
    /// </summary>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <param name="offset">The number of rows to skip.</param>
    public SqlPagingFragment(int limit, int offset)
    {
        Limit = limit;
        Offset = offset;
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}