namespace SqlInterpol.Schema;

/// <summary>
/// A typed column reference created from a property selector expression, mapping a physical
/// column name to a CLR property name.
/// </summary>
/// <param name="sourceReference">The entity reference that provides the table/alias prefix.</param>
/// <param name="columnName">The physical database column name (e.g., <c>PROD_ID</c>).</param>
/// <param name="propertyName">The CLR property name on the entity type (e.g., <c>Id</c>).</param>
public class SqlColumnReference(ISqlReference sourceReference, string columnName, string propertyName) 
    : SqlColumnReferenceBase(sourceReference)
{
    /// <summary>Gets the CLR property name (e.g., <c>Id</c>).</summary>
    public override string PropertyName { get; } = propertyName;
    
    /// <summary>Gets the physical database column name (e.g., <c>PROD_ID</c>).</summary>
    internal override string ColumnName { get; } = columnName;
}