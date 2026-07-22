using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// A fragment representing the full declaration of a SQL entity, natively rendering 
/// the entity along with its alias assignment using the active dialect's formatting rules.
/// </summary>
/// <param name="entity">The entity to declare.</param>
public class SqlDeclaration(ISqlEntityBase entity) : ISqlDeclaration
{
    /// <inheritdoc />
    public ISqlEntityBase Entity { get; } = entity;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // Force the entity to render in Declaration mode so it includes the full table name + alias
        return Entity.ToSql(context, SqlRenderMode.Declaration);
    }
}