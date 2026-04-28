// namespace SqlInterpol.Models;

// public class SqlSubqueryTable : Sql
// {
//     private readonly SqlQuery _query;
//     private readonly string _alias;

//     internal SqlSubqueryTable(SqlQuery query, string alias) 
//         : base($"({query.Sql.Trim()}) AS {alias}")
//     {
//         _query = query;
//         _alias = alias;
//     }

//     public SqlSubqueryColumn this[string columnName]
//     {
//         get => new SqlSubqueryColumn(() => _alias, columnName);
//     }

//     public override Dictionary<string, object?> EmbeddedParameters => _query.Parameters;

//     public string Alias => _alias;

//     public SqlQuery Query => _query;
// }