// using SqlInterpol.Enums;

// namespace SqlInterpol.Models;

// public class SqlQuery
// {
//     public string Sql { get; }
//     public Dictionary<string, object?> Parameters { get; }

//     internal SqlInterpolOptions Options { get; }

//     // Set by As() / As<T>() so that column references on this instance can read it lazily
//     internal string? _registeredAlias;

//     public SqlQuery(string sql, Dictionary<string, object?> parameters, SqlInterpolOptions? options = null)
//     {
//         Sql = sql;
//         Parameters = parameters;
//         Options = options ?? SqlInterpolOptions.ForDialect(SqlDialect.SqlServer);
//     }

//     public override string ToString() => Sql;

//     public SqlSubqueryTable As(string alias)
//     {
//         _registeredAlias = alias;
//         return new SqlSubqueryTable(this, alias);
//     }

//     public SqlQueryProject<T> Project<T>() => new(this);

//     public static implicit operator string(SqlQuery result) => result.Sql;
// }