using System.Linq.Expressions;

namespace SqlInterpol.Schema;

/// <summary>
/// Provides high-performance expression tree evaluation utilities for resolving mapped properties.
/// </summary>
public static class SqlExpressionHelper
{
    /// <summary>
    /// Extracts the property name from a member access lambda expression.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="expression">The expression targeting a specific property.</param>
    /// <returns>The name of the targeted property.</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is not a valid member access.</exception>
    public static string GetPropertyName<T>(Expression<Func<T, object?>> expression)
    {
        if (expression.Body is MemberExpression member) 
        {
            return member.Member.Name;
        }
        
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember) 
        {
            return unaryMember.Member.Name;
        }
        
        throw new ArgumentException($"Expression '{expression}' must be a direct property access.");
    }
}