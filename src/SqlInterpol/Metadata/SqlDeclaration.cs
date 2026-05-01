using SqlInterpol.Config;

namespace SqlInterpol.Metadata;

public class SqlDeclaration(ISqlReference reference) : ISqlDeclaration
{
    public ISqlReference Reference { get; } = reference;

    public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var sourceSql = Reference.Source.ToSql(context, SqlRenderMode.BaseName);

        return context.Dialect.ApplyAlias(sourceSql, Reference.Alias);
    }
}