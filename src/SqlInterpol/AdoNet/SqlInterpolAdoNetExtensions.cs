using System.Data;
using System.Data.Common;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;

namespace SqlInterpol.AdoNet;

public static class SqlInterpolAdoNetExtensions
{
    /// <summary>
    /// Resolvers for mapping DbConnection Types to a SQL Dialect.
    /// </summary>
    public static IList<Func<Type, ISqlDialect?>> ConnectionResolvers { get; } = [];

    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> whose dialect is automatically resolved from the connection.
    /// </summary>
    public static SqlBuilder CreateSqlBuilder(this IDbConnection connection, SqlInterpolOptions? options = null)
        => new(DetectDialect(connection), options);

    /// <summary>
    /// Automatically creates and binds parameters from a SqlQueryResult to an existing DbCommand.
    /// This delegates creation to the command's own factory, guaranteeing the correct concrete provider types.
    /// </summary>
    public static void BindParameters(this DbCommand command, SqlQueryResult result)
    {
        foreach (var kvp in result.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = kvp.Key;
            parameter.Value = kvp.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static ISqlDialect DetectDialect(IDbConnection connection)
    {
        var type = connection.GetType();
        var currentType = type;

        // 1. Check registered connection hierarchy resolvers
        while (currentType != null && currentType != typeof(object))
        {
            foreach (var resolver in ConnectionResolvers)
            {
                if (resolver(currentType) is ISqlDialect customDialect) return customDialect;
            }
            currentType = currentType.BaseType;
        }

        // 2. Check built-in connection hierarchy
        var dialect = TryMatchConnectionHierarchy(connection);

        return dialect ?? throw new NotSupportedException(
            $"The connection type '{type.Name}' is not automatically mapped to a known SQL dialect. " +
            "Instantiate SqlBuilder manually and provide a custom ISqlDialect, or register a resolver via SqlInterpolAdoNetExtensions.ConnectionResolvers.");
    }

    private static ISqlDialect? TryMatchConnectionHierarchy(IDbConnection connection)
    {
        var type = connection.GetType();
        while (type != null && type != typeof(object))
        {
            var dialect = TryMatchConnectionType(type);
            if (dialect != null) return dialect;
            type = type.BaseType;
        }
        return null;
    }

    private static ISqlDialect? TryMatchConnectionType(Type type) => type.Name switch
    {
        "SqlConnection" when type.Namespace is "Microsoft.Data.SqlClient" or "System.Data.SqlClient"
            => new SqlServerDialect(),
        "NpgsqlConnection"  => new PostgreSqlDialect(),
        "SqliteConnection"  => new SqLiteDialect(),
        "MySqlConnection"   => new MySqlDialect(),
        "OracleConnection"  => new OracleDialect(),
        "FbConnection"      => new FirebirdDialect(),
        _                   => null
    };
}