using SqlInterpol.Configuration;
using SqlInterpol.Dialects;

namespace SqlInterpol;

/// <summary>
/// Caches a single shared instance of a dialect to avoid repeated allocations.
/// </summary>
/// <typeparam name="T">The dialect implementation type to cache.</typeparam>
public static class SqlDialectCache<T> where T : ISqlDialect, new()
{
    /// <summary>The single shared instance of <typeparamref name="T"/>.</summary>
    public static readonly T Instance = new();
}

public partial class SqlBuilder
{
    /// <summary>
    /// Creates a builder configured for PostgreSQL.
    /// </summary>
    /// <param name="opt">Optional settings; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new builder targeting the PostgreSQL dialect.</returns>
    public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<PostgreSqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a builder configured for MySQL or MariaDB.
    /// </summary>
    /// <param name="opt">Optional settings; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new builder targeting the MySQL dialect.</returns>
    public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<MySqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a builder configured for Oracle.
    /// </summary>
    /// <param name="opt">Optional settings; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new builder targeting the Oracle dialect.</returns>
    public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<OracleDialect>.Instance, opt);

    /// <summary>
    /// Creates a builder configured for SQLite.
    /// </summary>
    /// <param name="opt">Optional settings; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new builder targeting the SQLite dialect.</returns>
    public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<SqLiteDialect>.Instance, opt);

    /// <summary>
    /// Creates a builder configured for Firebird.
    /// </summary>
    /// <param name="opt">Optional settings; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new builder targeting the Firebird dialect.</returns>
    public static SqlBuilder Firebird(SqlInterpolOptions? opt = null)
        => new(SqlDialectCache<FirebirdDialect>.Instance, opt);

    /// <summary>
    /// Creates a builder configured for SQL Server (T-SQL).
    /// </summary>
    /// <param name="opt">Optional settings; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new builder targeting the SQL Server dialect.</returns>
    public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<SqlServerDialect>.Instance, opt);

    /// <summary>
    /// Creates a builder configured for a custom or third-party dialect.
    /// </summary>
    /// <typeparam name="TDialect">The custom dialect implementation to use. Must have a public parameterless constructor.</typeparam>
    /// <param name="opt">Optional settings; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new builder targeting the specified dialect.</returns>
    public static SqlBuilder Dialect<TDialect>(SqlInterpolOptions? opt = null) 
        where TDialect : ISqlDialect, new()
        => new(SqlDialectCache<TDialect>.Instance, opt);
}