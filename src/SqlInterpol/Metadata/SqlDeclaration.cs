namespace SqlInterpol;

/// <summary>
/// Default implementation of <see cref="ISqlDeclaration"/> that delegates rendering
/// to the entity in <see cref="SqlRenderMode.Declaration"/> mode.
/// </summary>
/// <param name="entity">The entity this declaration wraps.</param>
public class SqlDeclaration(ISqlEntityBase entity) : ISqlDeclaration
{
    /// <inheritdoc />
    public ISqlEntityBase Entity { get; } = entity;

    /// <summary>
    /// Renders the entity as a full declaration (e.g. <c>"Products" AS "p"</c>).
    /// </summary>
    /// <param name="context">The active context providing dialect and options.</param>
    /// <param name="mode">Unused; rendering always uses <see cref="SqlRenderMode.Declaration"/>.</param>
    /// <returns>The declaration SQL string.</returns>
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return Entity.ToSql(context, SqlRenderMode.Declaration);
    }
}