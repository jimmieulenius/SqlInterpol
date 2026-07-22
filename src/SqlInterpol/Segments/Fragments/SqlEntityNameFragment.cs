using SqlInterpol.Configuration;
using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// A fragment that renders a single quoted identifier (name only, without schema or alias).[cite: 3]
/// </summary>
/// <param name="Entity">The entity this name is scoped to.</param>
/// <param name="Name">The identifier to quote and render.</param>
public record SqlEntityNameFragment(ISqlEntityBase Entity, string Name) : ISqlFragment
{
    /// <summary>
    /// Renders the identifier wrapped in the dialect's identifier quote characters.[cite: 3]
    /// </summary>
    /// <param name="context">The active context providing dialect quote characters.</param>
    /// <param name="mode">Unused; the name is always rendered as a bare quoted identifier.</param>[cite: 3]
    /// <returns>The quoted identifier (e.g. <c>"MyColumn"</c> or <c>[MyColumn]</c>).</returns>[cite: 3]
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        => $"{context.Dialect.OpenQuote}{Name}{context.Dialect.CloseQuote}";
}