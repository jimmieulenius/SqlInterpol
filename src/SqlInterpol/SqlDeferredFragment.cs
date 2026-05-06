using SqlInterpol.Config;

namespace SqlInterpol;

internal sealed class SqlDeferredFragment(Func<ISqlContext, string> renderer) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) 
        => renderer(context);
}