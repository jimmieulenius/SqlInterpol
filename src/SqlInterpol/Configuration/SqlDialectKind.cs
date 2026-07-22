namespace SqlInterpol.Configuration;

/// <summary>
/// A strongly-typed identifier for a SQL dialect vendor, used to select dialect-specific
/// behavior and options.
/// </summary>
/// <param name="Value">The string identifier for this dialect (e.g. <c>"PostgreSql"</c>).</param>
/// <remarks>
/// Implicit conversions to and from <see cref="string"/> allow dialect kinds to be compared
/// and assigned using plain strings when needed.
/// </remarks>
public readonly record struct SqlDialectKind(string Value)
{
    /// <summary>Firebird SQL dialect.</summary>
    public static SqlDialectKind Firebird { get; } = new("Firebird");

    /// <summary>MariaDB dialect.</summary>
    public static SqlDialectKind MariaDb { get; } = new("MariaDb");

    /// <summary>MySQL dialect.</summary>
    public static SqlDialectKind MySql { get; } = new("MySql");

    /// <summary>Oracle Database dialect.</summary>
    public static SqlDialectKind Oracle { get; } = new("Oracle");

    /// <summary>PostgreSQL dialect.</summary>
    public static SqlDialectKind PostgreSql { get; } = new("PostgreSql");

    /// <summary>SQLite dialect.</summary>
    public static SqlDialectKind SqLite { get; } = new("SqLite");

    /// <summary>Microsoft SQL Server dialect.</summary>
    public static SqlDialectKind SqlServer { get; } = new("SqlServer");

    /// <summary>ANSI SQL dialect.</summary>
    public static SqlDialectKind Ansi { get; } = new("Ansi");

    /// <summary>A generic dialect that uses square brackets for identifiers.</summary>
    public static SqlDialectKind GenericBracket { get; } = new("GenericBracket");

    /// <summary>A generic dialect that uses backticks for identifiers.</summary>
    public static SqlDialectKind GenericBacktick { get; } = new("GenericBacktick");

    /// <summary>Returns the string value of this dialect kind.</summary>
    public override string ToString() => Value;

    /// <summary>Implicitly converts a <see cref="SqlDialectKind"/> to its underlying string value.</summary>
    public static implicit operator string(SqlDialectKind kind) => kind.Value;

    /// <summary>Implicitly converts a string to a <see cref="SqlDialectKind"/>.</summary>
    public static implicit operator SqlDialectKind(string value) => new(value);
}