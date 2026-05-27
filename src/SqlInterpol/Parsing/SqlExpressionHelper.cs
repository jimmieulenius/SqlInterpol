using System.Linq.Expressions;
using System.Reflection;

namespace SqlInterpol.Parsing;

internal static class SqlExpressionHelper
{
    /// <summary>
    /// Unwraps the body of a lambda expression, stripping any compiler-generated 
    /// boxing conversions (e.g., converting value types to object).
    /// </summary>
    private static Expression UnwrapBody(LambdaExpression expression)
    {
        Expression body = expression.Body;

        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            return unary.Operand;
        }

        return body;
    }

    /// <summary>
    /// Extracts property names from a lambda expression, supporting both single properties 
    /// (e.g., x => x.Id) and anonymous objects (e.g., x => new { x.Id, x.TenantId }).
    /// </summary>
    public static string[] GetPropertyNames(LambdaExpression selector)
    {
        Expression body = UnwrapBody(selector);

        // 1. Single property (x => x.Id)
        if (body is MemberExpression memberExpr && memberExpr.Member is PropertyInfo)
        {
            return [memberExpr.Member.Name];
        }

        // 2. Anonymous object (x => new { x.Id, x.TenantId })
        if (body is NewExpression newExpr)
        {
            return newExpr.Members?.Select(m => m.Name).ToArray() ?? [];
        }

        throw new ArgumentException($"Expression '{selector}' must be a single property access or an anonymous object creation.");
    }

    /// <summary>
    /// Extracts the PropertyInfo from a lambda expression that selects a single property.
    /// </summary>
    public static PropertyInfo GetProperty(LambdaExpression propertySelector)
    {
        Expression body = UnwrapBody(propertySelector);

        if (body is MemberExpression member && member.Member is PropertyInfo propInfo)
        {
            return propInfo;
        }

        throw new ArgumentException($"Expression '{propertySelector}' is not a valid property selector. Ensure you are selecting a property and not a field or method.");
    }

    /// <summary>
    /// Extracts the property name from a lambda expression that selects a single property.
    /// </summary>
    public static string GetPropertyName(LambdaExpression propertySelector) 
        => GetProperty(propertySelector).Name;
}