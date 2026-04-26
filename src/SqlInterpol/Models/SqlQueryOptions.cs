using SqlInterpol.Enums;

namespace SqlInterpol.Models;

public class SqlQueryOptions
{
    public SqlDatabaseType Database { get; set; } = SqlDatabaseType.SqlServer;

    public string IdentifierStart { get; set; } = "[";

    public string IdentifierEnd { get; set; } = "]";

    public string ParameterPrefix { get; set; } = "@";

    public bool UsePositionalParameters { get; set; } = false;

    public int IndentSize { get; set; } = 2;

    public static SqlQueryOptions ForDatabase(SqlDatabaseType database) => database switch
    {
        SqlDatabaseType.SqlServer => new SqlQueryOptions 
        { 
            Database = SqlDatabaseType.SqlServer,
            IdentifierStart = "[",
            IdentifierEnd = "]",
            ParameterPrefix = "@",
            UsePositionalParameters = false
        },
        SqlDatabaseType.MySql => new SqlQueryOptions 
        { 
            Database = SqlDatabaseType.MySql,
            IdentifierStart = "`",
            IdentifierEnd = "`",
            ParameterPrefix = "@",
            UsePositionalParameters = false
        },
        SqlDatabaseType.PostgreSql => new SqlQueryOptions 
        { 
            Database = SqlDatabaseType.PostgreSql,
            IdentifierStart = "\"",
            IdentifierEnd = "\"",
            ParameterPrefix = "$",
            UsePositionalParameters = true
        },
        SqlDatabaseType.SQLite => new SqlQueryOptions 
        { 
            Database = SqlDatabaseType.SQLite,
            IdentifierStart = "\"",
            IdentifierEnd = "\"",
            ParameterPrefix = "?",
            UsePositionalParameters = true
        },
        SqlDatabaseType.Oracle => new SqlQueryOptions 
        { 
            Database = SqlDatabaseType.Oracle,
            IdentifierStart = "\"",
            IdentifierEnd = "\"",
            ParameterPrefix = ":",
            UsePositionalParameters = false
        },
        _ => new()
    };
}