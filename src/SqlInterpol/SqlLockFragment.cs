using SqlInterpol.Config;

namespace SqlInterpol;

public record SqlLockFragment(SqlLockMode Mode) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}