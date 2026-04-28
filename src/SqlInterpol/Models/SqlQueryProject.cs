// using System.Linq.Expressions;

// namespace SqlInterpol.Models;

// public class SqlQueryProject<T>
// {
//     private readonly SqlQuery _query;

//     internal SqlQueryProject(SqlQuery query)
//     {
//         _query = query;
//     }

//     public SqlSubqueryColumn this[Expression<Func<T, object?>> propertySelector]
//     {
//         get
//         {
//             var memberExpr = propertySelector.Body as MemberExpression ??
//                 ((UnaryExpression)propertySelector.Body).Operand as MemberExpression ??
//                 throw new ArgumentException("Expression must be a property access", nameof(propertySelector));

//             return new SqlSubqueryColumn(() => _query._registeredAlias, memberExpr.Member.Name);
//         }
//     }
// }