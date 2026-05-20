
namespace SqlInterpol;

public class SqlPagingFragment : ISqlFragment
{
    public int Limit { get; }
    public int Offset { get; }

    public SqlPagingFragment(int limit, int offset)
    {
        Limit = limit;
        Offset = offset;
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}