using System.Text;
using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlQuery(SqlBuilder builder, IReadOnlyList<SqlSegment> segments) : ISqlQuery
{
    public SqlBuilder Builder { get; } = builder;
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return Builder.Build(this).Sql;
    }

    public SqlQueryResult Build()
    {
        // PROXY: Let the builder handle the complex rendering and parameter extraction!
        return Builder.Build(this);
    }
}