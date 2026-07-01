namespace SqlInterpol
{
    /// <summary>
    /// A marker fragment that defers column resolution until the preprocessor pass.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SqlDynamicColumn"/> class.
    /// </remarks>
    /// <param name="entityType">The type of the target entity.</param>
    /// <param name="propertyName">The name of the target property.</param>
    public class SqlDynamicColumnFragment(Type entityType, string propertyName) : ISqlFragment
    {
        /// <summary>
        /// Gets the compile-time type of the POCO entity model.
        /// </summary>
        public Type EntityType { get; } = entityType;

        /// <summary>
        /// Gets the exact string name of the property to be resolved into a physical database column.
        /// </summary>
        public string PropertyName { get; } = propertyName;

        /// <inheritdoc />
        public string ToSql(ISqlContext context, SqlRenderMode renderMode)
        {
            // This marker relies on the preprocessor. If reached, return fallback.
            return string.Empty; 
        }
    }
}