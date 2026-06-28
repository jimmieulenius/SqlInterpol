using System.Text;

namespace SqlInterpol.Parsing;

/// <summary>
/// A structural AST fragment that wraps recursively preprocessed segments of a nested subquery,
/// preserving its foundational query interfaces and entity references for downstream compilers.
/// </summary>
public class SqlNestedQueryFragment : ISqlQueryFragment, ISqlEntityBase, ISqlSegmentContainer
{
    /// <summary>
    /// Gets the open collection of executable segments belonging to the subquery.
    /// </summary>
    public IReadOnlyList<SqlSegment> Segments { get; }

    /// <summary>
    /// Gets the entity reference tracking scope for this query context.
    /// </summary>
    public ISqlReference Reference { get; }

    /// <summary>
    /// Gets the full declaration fragment wrapper for this subquery node.
    /// </summary>
    public ISqlDeclaration Declaration { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this subquery fragment should exclude wrapping parentheses during rendering.
    /// </summary>
    public bool ExcludeParentheses { get; set; }

    /// <summary>
    /// Gets the underlying C# model type this entity represents.
    /// Resolves to the fragment's own type for dynamic structural subqueries.
    /// </summary>
    public Type ModelType => typeof(SqlNestedQueryFragment);

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlNestedQueryFragment"/> class safely bound to its own context.
    /// </summary>
    public SqlNestedQueryFragment(List<SqlSegment> segments, ISqlReference? reference)
    {
        Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        
        Reference = reference ?? new SqlEntityReference(this) { FallbackAlias = "Subquery" };
        Declaration = new SqlDeclaration(this);
    }

    /// <inheritdoc />
    public ISqlReference this[string columnName] => new SqlRawColumnReference(Reference, columnName);

    /// <inheritdoc />
    public ISqlFragment Column(string name) => new SqlEntityNameFragment(this, name);

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var aliasToUse = Reference?.Alias ?? Reference?.FallbackAlias;
        var escapedAlias = (Reference != null && Reference.IsAliasQuoted && aliasToUse != null) 
            ? context.Dialect.QuoteIdentifier(aliasToUse) 
            : aliasToUse;

        if (mode == SqlRenderMode.AliasOnly) return escapedAlias ?? string.Empty;
        if (mode == SqlRenderMode.AsAlias) return escapedAlias != null ? $"{SqlKeyword.As.Value} {escapedAlias}" : string.Empty;

        var sb = new StringBuilder();
        
        // Safely extract the configured renderer from the context, falling back to the singleton if unavailable
        var renderer = (context as SqlContext)?.Renderer ?? SqlSegmentRenderer.Instance;

        // FIX: Route inner segments through the official renderer to preserve RenderMode rules and lookahead logic!
        for (int i = 0; i < Segments.Count; i++)
        {
            var segment = Segments[i];
            var rendered = renderer.Render(context, segment, i, Segments);
            
            if (rendered != null)
            {
                sb.Append(rendered);
            }
        }
        
        string innerSql = sb.ToString();
        if (ExcludeParentheses) return innerSql;

        return mode switch
        {
            SqlRenderMode.Declaration => escapedAlias != null ? context.Dialect.ApplyAlias($"({innerSql})", escapedAlias) : $"({innerSql})",
            SqlRenderMode.BaseName => $"({innerSql})",
            _ => $"({innerSql})"
        };
    }
}