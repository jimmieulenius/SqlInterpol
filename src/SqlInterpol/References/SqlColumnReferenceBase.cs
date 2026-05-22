namespace SqlInterpol;

/// <summary>
/// Abstract base for column references, implementing mode-aware rendering and <see cref="ISqlProjection"/>.
/// </summary>
/// <param name="sourceReference">The entity reference this column belongs to, providing the table alias prefix.</param>
public abstract class SqlColumnReferenceBase(ISqlReference sourceReference) 
    : SqlReference(sourceReference.Source), ISqlProjection
{
    /// <summary>The entity reference that provides the table/alias prefix when rendering qualified column names.</summary>
    protected readonly ISqlReference SourceReference = sourceReference;

    /// <summary>Gets the CLR property name this projection maps to.</summary>
    public abstract string PropertyName { get; }

    /// <summary>Returns the physical column name for rendering.</summary>
    /// <param name="context">The active context providing dialect and options.</param>
    /// <returns>The physical column name to use in SQL output.</returns>
    protected abstract string GetColumnName(ISqlContext context);

    /// <summary>
    /// Renders the column reference according to the specified <paramref name="mode"/>.
    /// </summary>
    /// <param name="context">The active context providing dialect quoting.</param>
    /// <param name="mode">
    /// <see cref="SqlRenderMode.AliasOnly"/> emits just the quoted property name;
    /// <see cref="SqlRenderMode.BaseName"/> emits the quoted physical column name;
    /// default emits the fully qualified <c>"alias"."column"</c> form.
    /// </param>
    /// <returns>The SQL string for this column reference.</returns>
    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            SqlRenderMode.AliasOnly => context.Dialect.QuoteIdentifier(PropertyName),

            SqlRenderMode.BaseName => context.Dialect.QuoteIdentifier(GetColumnName(context)),
            
            _ => RenderFullReference(context)
        };
    }

    private string RenderFullReference(ISqlContext context)
    {
        var sourcePointer = SourceReference.ToSql(context);
        var columnName = GetColumnName(context);
        
        return $"{sourcePointer}.{context.Dialect.QuoteIdentifier(columnName)}";
    }
    
    ISqlReference ISqlProjection.Reference => this;
}