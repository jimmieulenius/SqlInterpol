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
    /// <typeparam name="T">The compile-time type of the entity POCO.</typeparam>
    /// <param name="entity">The entity instance.</param>
    /// <param name="propertyName">The exact name of the property to project.</param>
    /// <returns>A dynamic column marker resolved during preprocessing.</returns>
    public static SqlDynamicColumn Column<T>(this T entity, string propertyName) where T : class
    {
        return new SqlDynamicColumn(typeof(T), propertyName);
    }
}