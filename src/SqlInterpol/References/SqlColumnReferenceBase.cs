namespace SqlInterpol;

/// <summary>
/// Abstract base for column references, implementing mode-aware rendering and <see cref="ISqlProjection"/>.
/// </summary>
/// <param name="sourceReference">The entity reference this column belongs to, providing the table alias prefix.</param>
public abstract class SqlColumnReferenceBase(ISqlReference sourceReference) 
    : SqlReference(sourceReference.Source), ISqlProjection
{
    /// <summary>The entity reference that provides the table/alias prefix when rendering qualified column names.</summary>
    internal readonly ISqlReference SourceReference = sourceReference;

    /// <summary>Gets the CLR property name this projection maps to.</summary>
    public abstract string PropertyName { get; }

    /// <summary>Gets the physical column name for rendering.</summary>
    internal abstract string ColumnName { get; }

    /// <summary>
    /// Renders the column reference according to the specified <paramref name="mode"/>.
    /// </summary>
    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            SqlRenderMode.AliasOnly => context.Dialect.QuoteIdentifier(PropertyName),
            SqlRenderMode.BaseName => context.Dialect.QuoteIdentifier(ColumnName),
            _ => RenderFullReference(context)
        };
    }

    private string RenderFullReference(ISqlContext context)
    {
        // PROPER PREFIX RESOLUTION
        // Always try the Alias first, then fall back to the Base Name (Table Name).
        // We explicitly use AliasOnly so Subqueries do not accidentally render their full bodies!
        var sourcePointer = SourceReference.ToSql(context, SqlRenderMode.AliasOnly);
        
        if (string.IsNullOrWhiteSpace(sourcePointer))
            sourcePointer = SourceReference.ToSql(context, SqlRenderMode.BaseName);
            
        if (string.IsNullOrWhiteSpace(sourcePointer))
            sourcePointer = SourceReference.ToSql(context); // Ultimate fallback
        
        var columnName = ColumnName;
        
        return $"{sourcePointer}.{context.Dialect.QuoteIdentifier(columnName)}";
    }
    
    ISqlReference ISqlProjection.Reference => this;
}