// using System.Linq.Expressions;

// namespace SqlInterpol.Models;

// public class SqlTableDefinition<T> : SqlTable where T : class, new()
// {
//     private readonly Dictionary<string, SqlColumn> _columns;

//     public SqlTableDefinition(SqlTable table, Dictionary<string, SqlColumn> columns) 
//         : base(table.Name, table.Schema(), table.Alias())
//     {
//         // Recreate columns to reference this SqlTableDefinition instead of the original table
//         // This ensures that when we alias this table, the columns reflect the alias
//         _columns = [];

//         foreach (var (name, col) in columns)
//         {
//             _columns[name] = new SqlColumn(this, col.Name);
//         }
//     }

//     public SqlColumn this[Expression<Func<T, object?>> propertySelector]
//     {
//         get
//         {
//             var propertyName = GetPropertyName(propertySelector);

//             if (_columns.TryGetValue(propertyName, out var column))
//             {
//                 return column;
//             }

//             throw new InvalidOperationException($"Property {propertyName} not found or not marked with [SqlColumn]");
//         }
//     }

//     public SqlQuery Query(Func<SqlTableDefinition<T>, SqlQuery> buildQuery) => buildQuery(this);

//     public new SqlTableDefinition<T> As(string alias)
//     {
//         _alias = alias;

//         return this;
//     }

//     public SqlTableJoinPending InnerJoin(SqlReference otherTable)
//     {
//         var join = new SqlTableJoin(this);

//         return join.InnerJoin(otherTable);
//     }

//     public SqlTableJoinPending LeftJoin(SqlReference otherTable)
//     {
//         var join = new SqlTableJoin(this);

//         return join.LeftJoin(otherTable);
//     }

//     public SqlTableJoinPending RightJoin(SqlReference otherTable)
//     {
//         var join = new SqlTableJoin(this);

//         return join.RightJoin(otherTable);
//     }

//     public SqlTableJoinPending FullOuterJoin(SqlReference otherTable)
//     {
//         var join = new SqlTableJoin(this);

//         return join.FullOuterJoin(otherTable);
//     }

//     public SqlTableJoin CrossJoin(SqlReference otherTable)
//     {
//         var join = new SqlTableJoin(this);

//         return join.CrossJoin(otherTable);
//     }

//     private static string GetPropertyName(Expression<Func<T, object?>> expression)
//     {
//         var memberExpr = expression.Body as MemberExpression ?? 
//             ((UnaryExpression)expression.Body).Operand as MemberExpression ??
//             throw new ArgumentException("Expression must be a property access");
        
//         return memberExpr.Member.Name;
//     }
// }