using SqlInterpol.Config;

namespace SqlInterpol.References;

public class SqlRawColumnReference(ISqlReference sourceReference, string columnName) 
    : SqlColumnReferenceBase(sourceReference)
{
    // For raw columns, the PropertyName falls back to the column name
    public override string PropertyName => columnName;

    protected override string GetColumnName(SqlContext context) => columnName;
}