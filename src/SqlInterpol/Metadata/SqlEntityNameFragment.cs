namespace SqlInterpol;

/// <summary>
/// A fragment that renders a single quoted identifier (name only, without schema or alias).
/// </summary>
/// <param name="Entity">The entity this name is scoped to.</param>
/// <param name="Name">The identifier to quote and render.</param>
public record SqlEntityNameFragment(ISqlEntityBase Entity, string Name) : ISqlFragment
{
    /// <summary>
    /// Renders the identifier wrapped in the dialect's identifier quote characters.
    /// </summary>
    /// <param name="context">The active context providing dialect quote characters.</param>
    /// <param name="mode">Unused; the name is always rendered as a bare quoted identifier.</param>
    /// <returns>The quoted identifier (e.g. <c>"MyColumn"</c> or <c>[MyColumn]</c>).</returns>
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        => $"{context.Dialect.OpenQuote}{Name}{context.Dialect.CloseQuote}";
}