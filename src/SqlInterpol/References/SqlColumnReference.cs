using SqlInterpol.Config;

namespace SqlInterpol.References;

public class SqlColumnReference(ISqlReference sourceReference, string columnName, string propertyName) 
    : SqlColumnReferenceBase(sourceReference)
{
    // The C# name (e.g., "Id")
    public override string PropertyName => propertyName;

    // The DB name (e.g., "PROD_ID")
    protected override string GetColumnName(SqlContext context) => columnName;
}