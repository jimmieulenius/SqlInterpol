using SqlInterpol.Enums;

namespace SqlInterpol.Models;

public class SqlInterpolOptions
{
    public SqlDialect Dialect { get; set; } = SqlDialect.SqlServer;

    public string IdentifierStart { get; set; } = "[";

    public string IdentifierEnd { get; set; } = "]";

    public string ParameterPrefix { get; set; } = "@";

    public bool UsePositionalParameters { get; set; } = false;

    public int IndentSize { get; set; } = 2;

    public static SqlInterpolOptions ForDialect(SqlDialect dialect) => dialect switch
    {
        SqlDialect.SqlServer => new SqlInterpolOptions 
        { 
            Dialect = SqlDialect.SqlServer,
            IdentifierStart = "[",
            IdentifierEnd = "]",
            ParameterPrefix = "@",
            UsePositionalParameters = false
        },
        SqlDialect.MySql => new SqlInterpolOptions 
        { 
            Dialect = SqlDialect.MySql,
            IdentifierStart = "`",
            IdentifierEnd = "`",
            ParameterPrefix = "@",
            UsePositionalParameters = false
        },
        SqlDialect.PostgreSql => new SqlInterpolOptions 
        { 
            Dialect = SqlDialect.PostgreSql,
            IdentifierStart = "\"",
            IdentifierEnd = "\"",
            ParameterPrefix = "$",
            UsePositionalParameters = true
        },
        SqlDialect.SqLite => new SqlInterpolOptions 
        { 
            Dialect = SqlDialect.SqLite,
            IdentifierStart = "\"",
            IdentifierEnd = "\"",
            ParameterPrefix = "?",
            UsePositionalParameters = true
        },
        SqlDialect.Oracle => new SqlInterpolOptions 
        { 
            Dialect = SqlDialect.Oracle,
            IdentifierStart = "\"",
            IdentifierEnd = "\"",
            ParameterPrefix = ":",
            UsePositionalParameters = false
        },
        _ => new()
    };
}