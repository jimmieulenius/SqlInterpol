using SqlInterpol.Abstractions;

namespace SqlInterpol.Models;

public class SqlRawColumnReference : SqlColumnBase
{
    private readonly string _columnName;

    public SqlRawColumnReference(ISqlReference sourceReference, string columnName) 
        : base(sourceReference)
    {
        _columnName = columnName;
    }

    protected override string GetColumnName(SqlContext context) => _columnName;
}