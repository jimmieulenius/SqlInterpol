using SqlInterpol.Config;

namespace SqlInterpol.Metadata;

public class SqlDeclaration(ISqlEntityBase entity) : ISqlDeclaration
{
    public ISqlEntityBase Entity { get; } = entity;

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return Entity.ToSql(context, SqlRenderMode.Declaration);
    }
}


// using SqlInterpol.Config;

// namespace SqlInterpol.Metadata;

// public class SqlDeclaration(ISqlReference reference) : ISqlDeclaration
// {
//     public ISqlReference Reference { get; } = reference;

//     public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
//     {
//         var sourceSql = Reference.Source.ToSql(context, SqlRenderMode.BaseName);

//         return context.Dialect.ApplyAlias(sourceSql, Reference.Alias);
//     }
// }