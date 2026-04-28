using SqlInterpol.Config;

namespace SqlInterpol.References;

public class SqlRawColumnReference : SqlColumnReferenceBase
{
    private readonly string _columnName;

    public SqlRawColumnReference(ISqlReference sourceReference, string columnName) 
        : base(sourceReference)
    {
        _columnName = columnName;
    }

    protected override string GetColumnName(SqlContext context) => _columnName;
}