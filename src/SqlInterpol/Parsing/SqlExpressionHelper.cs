using System.Linq.Expressions;

namespace SqlInterpol.Parsing;

internal static class SqlExpressionHelper
{
    public static string GetMemberName(LambdaExpression propertySelector)
    {
        // Use .Body to get the actual logic (e.g., p.Name)
        Expression body = propertySelector.Body;

        // Strip out 'Convert' operations (boxing for value types like int/DateTime)
        if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
        {
            body = unary.Operand;
        }

        // Check if it's a member access (property or field)
        if (body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException($"Expression '{propertySelector}' is not a valid property selector.");
    }
}