using SqlInterpol.Config;

namespace SqlInterpol;

public interface ISqlFragment
{
    string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}