// using SqlInterpol.Config;
// using SqlInterpol.Dialects;

// namespace SqlInterpol;

// public partial class SqlBuilder
// {
//     public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
//         => new(new MySqlSqlDialect(), opt);

//     public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
//         => new(new OracleSqlDialect(), opt);

//     public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
//         => new(new PostgreSqlSqlDialect(), opt);

//     public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
//         => new(new SqLiteSqlDialect(), opt);

//     public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
//         => new(new SqlServerSqlDialect(), opt);

//     public static SqlBuilder ForDialect<T>(SqlInterpolOptions? opt = null) where T : ISqlDialect, new()
//         => new(new T(), opt);
// }