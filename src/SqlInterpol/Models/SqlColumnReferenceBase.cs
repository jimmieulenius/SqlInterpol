using SqlInterpol.Abstractions;

namespace SqlInterpol.Models;

public abstract class SqlColumnBase(ISqlReference sourceReference) : SqlReference(sourceReference.Source)
{
    protected readonly ISqlReference SourceReference = sourceReference;

    // Every column must eventually provide a name string
    protected abstract string GetColumnName(SqlContext context);

    public override string ToSql(SqlContext context)
    {
        var sourcePointer = SourceReference.ToSql(context);
        var columnName = GetColumnName(context);
        
        return $"{sourcePointer}.{context.Dialect.QuoteIdentifier(columnName)}";
    }
}