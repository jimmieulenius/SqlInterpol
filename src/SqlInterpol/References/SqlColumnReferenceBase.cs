using SqlInterpol.Config;
using SqlInterpol.Metadata;

namespace SqlInterpol.References;

// We add ISqlProjection here so all derived columns (typed or raw) 
// can participate in the "AS" aliasing magic.
public abstract class SqlColumnReferenceBase(ISqlReference sourceReference) 
    : SqlReference(sourceReference.Source), ISqlProjection
{
    protected readonly ISqlReference SourceReference = sourceReference;

    // This satisfies ISqlProjection. 
    // It tells the builder what the 'C#' name of this column is.
    public abstract string PropertyName { get; }

    // Every column must eventually provide a DB name string (e.g. "PROD_ID")
    protected abstract string GetColumnName(SqlContext context);

    public override string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            // The "AS [Name]" part of a SELECT
            SqlRenderMode.AliasOnly => 
                context.Dialect.QuoteIdentifier(PropertyName),

            // The "WHERE [prd].[PROD_NAME]" part
            _ => RenderFullReference(context)
        };
    }

    private string RenderFullReference(SqlContext context)
    {
        // 1. Get the prefix (e.g., "[p]")
        var sourcePointer = SourceReference.ToSql(context);
        
        // 2. Get the actual column name (e.g., "PROD_ID")
        var columnName = GetColumnName(context);
        
        // 3. Combine them: [p].[PROD_ID]
        return $"{sourcePointer}.{context.Dialect.QuoteIdentifier(columnName)}";
    }
    
    // Explicit implementation if needed, though Reference is usually enough
    ISqlReference ISqlProjection.Reference => this;
    ISqlDeclaration ISqlProjection.Declaration => throw new NotSupportedException("Columns do not have declarations.");
}