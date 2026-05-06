using SqlInterpol.Config;

namespace SqlInterpol;

public interface ISqlFragment
{
    string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}