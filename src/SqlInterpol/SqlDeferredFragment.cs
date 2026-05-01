using SqlInterpol.Config;

namespace SqlInterpol;

internal sealed class SqlDeferredFragment(Func<SqlContext, string> renderer) : ISqlFragment
{
    public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default) 
        => renderer(context);
}