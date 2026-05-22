using System.Linq.Expressions;
using System.Reflection;

namespace SqlInterpol.Parsing;

internal static class SqlExpressionHelper
{
    public static MemberInfo GetMember(LambdaExpression propertySelector)
    {
        Expression body = propertySelector.Body;

        // Strip 'Convert' operations (boxing for value types)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member)
        {
            return member.Member;
        }

        throw new ArgumentException($"Expression '{propertySelector}' is not a valid property selector.");
    }

    public static string GetMemberName(LambdaExpression propertySelector) 
        => GetMember(propertySelector).Name;
}