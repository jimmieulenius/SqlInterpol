// namespace SqlInterpol;

// /// <summary>
// /// Enumerates optional SQL features that may or may not be supported by a given dialect.
// /// </summary>
// /// <remarks>
// /// Each value corresponds to a SQL construct that is absent from some dialects.
// /// When a query uses a fragment that implements <see cref="ISqlFeatureRequirement"/>,
// /// <see cref="SqlBuilder.Build(bool)"/> validates that the required feature is present in
// /// <see cref="ISqlDialect.SupportedFeatures"/> before rendering.
// /// </remarks>
// /// <seealso cref="ISqlDialect.SupportedFeatures"/>
// public enum SqlFeature
// {
//     /// <summary><c>FOR UPDATE</c> row-level locking. Supported by PostgreSQL, MySQL, Oracle.</summary>
//     ForUpdate,

//     /// <summary><c>FOR SHARE</c> shared row-level locking. Supported by PostgreSQL.</summary>
//     ForShare,

//     /// <summary><c>RETURNING</c> clause for retrieving values from DML statements. Supported by PostgreSQL, Firebird.</summary>
//     Returning,

//     /// <summary><c>ON CONFLICT</c> upsert syntax. Supported by PostgreSQL, SQLite.</summary>
//     OnConflict,

//     /// <summary><c>SELECT INTO</c> for creating a new table from a query result. Supported by SQL Server, PostgreSQL.</summary>
//     SelectInto,

//     /// <summary><c>DELETE</c> statement with multiple tables (e.g. DELETE FROM ... FROM ...).</summary>
//     MultiTableDelete,

//     /// <summary><c>UPDATE</c> statement with multiple tables (e.g. UPDATE ... FROM ...).</summary>
//     MultiTableUpdate,

//     /// <summary><c>DELETE</c> statement with a target table alias. Supported natively by PostgreSQL, and transpiled by SQL Server/MySQL.</summary>
//     DeleteAs,

//     /// <summary><c>UPDATE</c> statement with a target table alias. Supported natively by PostgreSQL and MySQL.</summary>
//     UpdateAs,

//     /// <summary>
//     /// Indicates the dialect natively supports updatable inline views (e.g., UPDATE (SELECT ...) SET ...).
//     /// If unsupported, the AST rendering engine will automatically safely rewrite the inline view into a CTE.
//     /// Supported natively by MySQL and Oracle.
//     /// </summary>
//     UpdatableInlineViews,

//     CreateTableAsSelect
// }