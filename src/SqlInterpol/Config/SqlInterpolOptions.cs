using SqlInterpol.Dialects;

namespace SqlInterpol.Config;

public record SqlInterpolOptions
{
    public SqlDialectKind Dialect { get; init; } = SqlDialectKind.SqlServer;
    public int ParameterIndexStart { get; init; } = 0;
    public string? ParameterPrefixOverride { get; init; }
    public ISqlParser? Parser { get; init; }
    public ISqlSegmentRenderer? Renderer { get; init; }

    public static SqlInterpolOptions GetDefault(ISqlDialect dialect)
    {
        return dialect switch
        {
            // Postgres standard is $1, $2, etc.
            PostgreSqlSqlDialect => new() { ParameterIndexStart = 1 },
            
            // Oracle often uses :0 or :1 depending on the driver, 
            // but we'll stick to 0-based as a safe default.
            OracleSqlDialect => new() { ParameterIndexStart = 0 },
            
            // Fallback for SQL Server, MySql, SQLite
            _ => new() 
        };
    }
}