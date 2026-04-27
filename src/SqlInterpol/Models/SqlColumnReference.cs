using SqlInterpol.Abstractions;

namespace SqlInterpol.Models;

public class SqlColumnReference(ISqlReference parentReference, string columnName) : SqlReference(parentReference.Source)
{
    private readonly ISqlReference _parentReference = parentReference;
    private readonly string _columnName = columnName;

    public override string ToSql(SqlContext context)
    {
        // Get the parent pointer (either "p" or "dbo.Product")
        var parentPath = _parentReference.ToSql(context);
        
        // Return "p.Name"
        return $"{parentPath}.{context.Dialect.QuoteIdentifier(_columnName)}";
    }
}