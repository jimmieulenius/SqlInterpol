using SqlInterpol.Dialects;

namespace SqlInterpol;

/// <summary>
/// Caches a single shared instance of a dialect to avoid repeated allocations.
/// </summary>
/// <typeparam name="T">The <see cref="ISqlDialect"/> implementation to cache.</typeparam>
public static class SqlDialectCache<T> where T : ISqlDialect, new()
{
    /// <summary>The single shared instance of <typeparamref name="T"/>.</summary>
    public static readonly T Instance = new();
}

public partial class SqlBuilder
{
    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> configured for PostgreSQL.
    /// </summary>
    /// <param name="opt">Optional options; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="SqlBuilder"/> targeting the PostgreSQL dialect.</returns>
    public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<PostgreSqlSqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> configured for MySQL / MariaDB.
    /// </summary>
    /// <param name="opt">Optional options; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="SqlBuilder"/> targeting the MySQL dialect.</returns>
    public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<MySqlSqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> configured for Oracle.
    /// </summary>
    /// <param name="opt">Optional options; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="SqlBuilder"/> targeting the Oracle dialect.</returns>
    public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<OracleSqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> configured for SQLite.
    /// </summary>
    /// <param name="opt">Optional options; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="SqlBuilder"/> targeting the SQLite dialect.</returns>
    public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<SqLiteSqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> configured for Firebird.
    /// </summary>
    /// <param name="opt">Optional options; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="SqlBuilder"/> targeting the Firebird dialect.</returns>
    public static SqlBuilder Firebird(SqlInterpolOptions? opt = null)
        => new(SqlDialectCache<FirebirdSqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> configured for SQL Server (T-SQL).
    /// </summary>
    /// <param name="opt">Optional options; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="SqlBuilder"/> targeting the SQL Server dialect.</returns>
    public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<SqlServerSqlDialect>.Instance, opt);

    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> configured for a custom or third-party dialect.
    /// </summary>
    /// <typeparam name="TDialect">
    /// The <see cref="ISqlDialect"/> implementation to use. Must have a public parameterless constructor.
    /// </typeparam>
    /// <param name="opt">Optional options; dialect-default settings are used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="SqlBuilder"/> targeting the specified dialect.</returns>
    public static SqlBuilder Dialect<TDialect>(SqlInterpolOptions? opt = null) 
        where TDialect : ISqlDialect, new()
        => new(SqlDialectCache<TDialect>.Instance, opt);
}