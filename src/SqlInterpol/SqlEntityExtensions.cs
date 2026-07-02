using System;
using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Provides fluent extension methods for SQL entities.
/// </summary>
public static class SqlEntityExtensions
{
    /// <summary>
    /// Dynamically defers a SQL column reference using a string property name.
    /// This allows dynamic field projection without requiring string indexers on POCO models.
    /// </summary>
    public static SqlDynamicColumnFragment Column<T>(this T entity, string propertyName) where T : class
    {
        return new SqlDynamicColumnFragment(typeof(T), propertyName);
    }

    /// <summary>
    /// Creates a dynamic SQL ORDER BY fragment using a string property name (Default Direction).
    /// </summary>
    public static ISqlOrderFragment OrderBy<T>(this T entity, string propertyName) where T : class
    {
        return new SqlDynamicOrderFragment(new SqlDynamicColumnFragment(typeof(T), propertyName));
    }

    /// <summary>
    /// Creates a dynamic SQL ORDER BY fragment using a string property name with an explicit direction.
    /// </summary>
    public static ISqlOrderFragment OrderBy<T>(this T entity, string propertyName, SqlOrderDirection direction) where T : class
    {
        return new SqlDynamicOrderFragment(new SqlDynamicColumnFragment(typeof(T), propertyName), direction);
    }

    /// <summary>
    /// Creates a SQL ORDER BY fragment using a strongly-typed member expression (Default Direction).
    /// </summary>
    public static ISqlOrderFragment OrderBy<T>(this T entity, Expression<Func<T, object?>> expression) where T : class
    {
        return entity.OrderBy(SqlExpressionHelper.GetPropertyName(expression));
    }

    /// <summary>
    /// Creates a SQL ORDER BY fragment using a strongly-typed member expression with an explicit direction.
    /// </summary>
    public static ISqlOrderFragment OrderBy<T>(this T entity, Expression<Func<T, object?>> expression, SqlOrderDirection direction) where T : class
    {
        return entity.OrderBy(SqlExpressionHelper.GetPropertyName(expression), direction);
    }
}