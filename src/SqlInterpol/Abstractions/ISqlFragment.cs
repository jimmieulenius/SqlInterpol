using SqlInterpol.Models;

namespace SqlInterpol.Abstractions;

public interface ISqlFragment
{
    string ToSql(SqlContext context);
}