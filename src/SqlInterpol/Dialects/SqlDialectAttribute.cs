namespace SqlInterpol.Dialects;

/// <summary>
/// Provides compile-time metadata for a SQL dialect, which is required by the AOT source generator.
/// By applying this attribute to an <c>ISqlDialect</c> implementation, the compiler can securely 
/// extract dialect-specific quoting rules during the build phase without needing to execute runtime code.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SqlDialectAttribute : Attribute
{
    /// <summary>
    /// Gets the opening identifier quote character used by this dialect.
    /// <para>Examples: <c>[</c> (SQL Server), <c>"</c> (PostgreSQL/SQLite), or <c>`</c> (MySQL).</para>
    /// </summary>
    public required string OpenQuote { get; init; }

    /// <summary>
    /// Gets the closing identifier quote character used by this dialect.
    /// <para>Examples: <c>]</c> (SQL Server), <c>"</c> (PostgreSQL/SQLite), or <c>`</c> (MySQL).</para>
    /// </summary>
    public required string CloseQuote { get; init; }
}