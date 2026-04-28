using SqlInterpol.Config;

namespace SqlInterpol.References;

public class SqlColumnReference(ISqlReference sourceReference, string columnName) 
    : SqlColumnReferenceBase(sourceReference)
{
    private readonly string _columnName = columnName;

    protected override string GetColumnName(SqlContext context) => _columnName;
}