using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// The reference fragment for a SQL entity, rendering as the alias (quoted or unquoted)
/// when one is set, or delegating to the parent entity fragment otherwise.
/// </summary>
/// <param name="parent">The entity fragment this reference belongs to.</param>
public class SqlEntityReference(ISqlFragment parent) : ISqlReference, ISqlAliasable
{
    private readonly ISqlFragment _parent = parent;

    /// <summary>Gets or sets the alias to use when this reference appears in query clauses.</summary>
    public string? Alias { get; set; }

    /// <summary>Gets or sets whether the alias should be wrapped in dialect-specific identifier quotes.</summary>
    public bool IsAliasQuoted { get; set; }

    /// <summary>Gets the alias to use when no explicit alias is set.</summary>
    public required string FallbackAlias { get; init; }

    /// <summary>Gets the entity fragment this reference belongs to.</summary>
    public ISqlFragment Source => _parent;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (!string.IsNullOrEmpty(Alias))
        {
            return IsAliasQuoted ? context.Dialect.QuoteIdentifier(Alias) : Alias;
        }
        
        return _parent.ToSql(context, mode);
    }
}