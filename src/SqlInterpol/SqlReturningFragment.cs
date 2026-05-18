using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlReturningFragment(params ISqlProjection[] columns) : ISqlFragment, ISqlFeatureRequirement
{
    public IReadOnlyList<ISqlProjection> Columns { get; } = columns;
    public SqlFeature RequiredFeature => SqlFeature.Returning;
    public string FeatureName => "RETURNING";

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