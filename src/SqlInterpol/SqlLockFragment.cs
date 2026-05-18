using SqlInterpol.Config;

namespace SqlInterpol;

public record SqlLockFragment(SqlLockMode Mode) : ISqlFragment, ISqlFeatureRequirement
{
    public SqlFeature RequiredFeature => Mode == SqlLockMode.Update ? SqlFeature.ForUpdate : SqlFeature.ForShare;
    public string FeatureName => Mode == SqlLockMode.Update ? "FOR UPDATE" : "FOR SHARE";

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}