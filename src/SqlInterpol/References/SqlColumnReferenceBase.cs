using SqlInterpol.Config;
using SqlInterpol.Metadata;

namespace SqlInterpol.References;

public abstract class SqlColumnReferenceBase(ISqlReference sourceReference) 
    : SqlReference(sourceReference.Source), ISqlProjection
{
    protected readonly ISqlReference SourceReference = sourceReference;

    public abstract string PropertyName { get; }

    protected abstract string GetColumnName(SqlContext context);

    public override string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            SqlRenderMode.AliasOnly => 
                context.Dialect.QuoteIdentifier(PropertyName),

            _ => RenderFullReference(context)
        };
    }

    private string RenderFullReference(SqlContext context)
    {
        var sourcePointer = SourceReference.ToSql(context);
        var columnName = GetColumnName(context);
        
        return $"{sourcePointer}.{context.Dialect.QuoteIdentifier(columnName)}";
    }
    
    ISqlReference ISqlProjection.Reference => this;
}