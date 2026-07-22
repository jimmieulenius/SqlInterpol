namespace SqlInterpol.Configuration;

/// <summary>
/// Enumerates optional SQL features that may or may not be supported by a given dialect.
/// </summary>
public enum SqlFeature
{
    /// <summary><c>FOR UPDATE</c> row-level locking. Supported by PostgreSQL, MySQL, Oracle.</summary>
    ForUpdate,

    /// <summary><c>FOR SHARE</c> shared row-level locking. Supported by PostgreSQL.</summary>
    ForShare,

    /// <summary><c>RETURNING</c> clause for retrieving values from DML statements. Supported by PostgreSQL, Firebird.</summary>
    Returning,

    /// <summary><c>ON CONFLICT</c> upsert syntax. Supported by PostgreSQL, SQLite.</summary>
    OnConflict,

    /// <summary><c>SELECT INTO</c> for creating a new table from a query result. Supported by SQL Server, PostgreSQL.</summary>
    SelectInto,

    /// <summary><c>DELETE</c> statement with multiple tables (e.g. DELETE FROM ... FROM ...).</summary>
    MultiTableDelete,

    /// <summary><c>UPDATE</c> statement with multiple tables (e.g. UPDATE ... FROM ...).</summary>
    MultiTableUpdate,

    /// <summary><c>DELETE</c> statement with a target table alias. Supported natively by PostgreSQL, and transpiled by SQL Server/MySQL.</summary>
    DeleteAs,

    /// <summary><c>UPDATE</c> statement with a target table alias. Supported natively by PostgreSQL and MySQL.</summary>
    UpdateAs,

    /// <summary>
    /// Indicates the dialect natively supports updatable inline views (e.g., UPDATE (SELECT ...) SET ...).
    /// If unsupported, the rendering engine will automatically safely rewrite the inline view into a CTE.
    /// Supported natively by MySQL and Oracle.
    /// </summary>
    UpdatableInlineViews,

    /// <summary>
    /// <c>CREATE TABLE AS SELECT</c> syntax. 
    /// Used as a fallback for dialects that do not support <c>SELECT INTO</c>.
    /// </summary>
    CreateTableAsSelect
}