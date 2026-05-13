using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlReturningFragment(params ISqlProjection[] columns) : ISqlFragment
{
    public IReadOnlyList<ISqlProjection> Columns { get; } = columns;

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        if (Columns.Count == 0)
        {
            return string.Empty;
        }

        var cols = string.Join(", ", Columns.Select(c => c.ToSql(context, SqlRenderMode.BaseName)));

        return $"{SqlKeyword.Returning} {cols}";
    }
}