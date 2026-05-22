namespace SqlInterpol;

/// <summary>
/// The reference fragment for a SQL entity, rendering as the alias (quoted or unquoted)
/// when one is set, or delegating to the parent entity fragment otherwise.
/// </summary>
/// <param name="parent">The entity fragment this reference belongs to.</param>
public class SqlEntityReference(ISqlFragment parent) : ISqlReference
{
    private readonly ISqlFragment _parent = parent;
    
    /// <summary>Gets or sets the alias to use when this reference appears in query clauses (e.g. <c>p</c> for <c>"Products" AS "p"</c>).</summary>
    public string? Alias { get; set; }

    /// <summary>Gets or sets whether <see cref="Alias"/> should be wrapped in dialect-specific identifier quotes.</summary>
    public bool IsAliasQuoted { get; set; }

    /// <summary>Gets or sets the alias to use when <see cref="Alias"/> is <see langword="null"/>.</summary>
    public required string FallbackAlias { get; set; }

    /// <summary>Gets the entity fragment this reference belongs to.</summary>
    public ISqlFragment Source => _parent;

    /// <summary>
    /// Renders the entity reference. When an alias is set, returns the alias (quoted or unquoted
    /// based on <see cref="IsAliasQuoted"/>); otherwise delegates to the parent entity fragment.
    /// </summary>
    /// <param name="context">The active context providing dialect quoting.</param>
    /// <param name="mode">The render mode forwarded to the parent fragment when no alias is set.</param>
    /// <returns>The SQL string for this entity reference.</returns>
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (!string.IsNullOrEmpty(Alias))
        {
            return IsAliasQuoted ? context.Dialect.QuoteIdentifier(Alias) : Alias;
        }

        return _parent.ToSql(context, mode);
    }
}