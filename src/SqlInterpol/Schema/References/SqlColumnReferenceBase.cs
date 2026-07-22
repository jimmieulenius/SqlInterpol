using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// Abstract base for column references, implementing mode-aware rendering and projection.
/// </summary>
/// <param name="sourceReference">The entity reference this column belongs to, providing the table alias prefix.</param>
public abstract class SqlColumnReferenceBase(ISqlReference sourceReference) 
    : SqlReference(sourceReference.Source), ISqlProjection
{
    /// <summary>
    /// The entity reference that provides the table/alias prefix when rendering qualified column names.
    /// </summary>
    internal readonly ISqlReference SourceReference = sourceReference;

    /// <summary>Gets the CLR property name this projection maps to.</summary>
    public abstract string PropertyName { get; }

    /// <summary>Gets the physical column name for rendering.</summary>
    internal abstract string ColumnName { get; }

    /// <inheritdoc />
    ISqlReference ISqlProjection.Reference => this;

    /// <summary>
    /// Renders the column reference according to the specified render mode.
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
        string sourcePointer;
        
        if (!string.IsNullOrEmpty(SourceReference.Alias))
        {
            sourcePointer = SourceReference.ToSql(context, SqlRenderMode.AliasOnly);
        }
        else
        {
            sourcePointer = SourceReference.ToSql(context, SqlRenderMode.BaseName);
        }
        
        if (string.IsNullOrWhiteSpace(sourcePointer))
        {
            sourcePointer = SourceReference.ToSql(context);
        }
        
        return $"{sourcePointer}.{context.Dialect.QuoteIdentifier(ColumnName)}";
    }
}