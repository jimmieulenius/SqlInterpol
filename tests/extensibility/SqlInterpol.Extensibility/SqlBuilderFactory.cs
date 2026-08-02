#if !CSHARP14_EXTENSION_TYPES
using SqlInterpol.Configuration;
using SqlInterpol.Extensibility.Dialects;

namespace SqlInterpol.Extensibility;

public static class SqlBuilderFactory
{
    public static SqlBuilder CustomDb(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<CustomDbSqlDialect>.Instance, opt);
}
#endif