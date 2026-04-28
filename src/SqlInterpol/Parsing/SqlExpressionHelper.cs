using System.Linq.Expressions;
using System.Reflection;

namespace SqlInterpol.Parsing;

internal static class SqlExpressionHelper
{
    // The "Engine": Gets the actual MemberInfo (PropertyInfo/FieldInfo)
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

    // The Wrapper: Just returns the name string
    public static string GetMemberName(LambdaExpression propertySelector) 
        => GetMember(propertySelector).Name;
}