
namespace SqlInterpol;

public class SqlDeclaration(ISqlEntityBase entity) : ISqlDeclaration
{
    public ISqlEntityBase Entity { get; } = entity;

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return Entity.ToSql(context, SqlRenderMode.Declaration);
    }
}