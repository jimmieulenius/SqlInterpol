namespace SqlInterpol.Schema;

/// <summary>
/// A column reference created from a raw string column name, where the property name
/// and physical column name are identical.
/// </summary>
/// <param name="sourceReference">The entity reference that provides the table/alias prefix.</param>
/// <param name="columnName">The physical column name, used as both the column name and the property name.</param>
public class SqlRawColumnReference(ISqlReference sourceReference, string columnName) 
    : SqlColumnReferenceBase(sourceReference)
{
    /// <summary>Gets the column name, which doubles as the property name for raw string-based references.</summary>
    public override string PropertyName { get; } = columnName;

    /// <inheritdoc />
    internal override string ColumnName { get; } = columnName;
}