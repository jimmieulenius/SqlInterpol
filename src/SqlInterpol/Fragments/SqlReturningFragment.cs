
namespace SqlInterpol;

/// <summary>
/// Represents a <c>RETURNING</c> clause that emits the specified columns after an
/// INSERT, UPDATE, or DELETE statement.
/// </summary>
/// <param name="columns">The projected columns to include in the RETURNING clause.</param>
public class SqlReturningFragment(params ISqlProjection[] columns) : ISqlFragment, ISqlFeatureRequirement
{
    /// <summary>Gets the columns to project in the RETURNING clause.</summary>
    public IReadOnlyList<ISqlProjection> Columns { get; } = columns;

    /// <inheritdoc />
    public SqlFeature RequiredFeature => SqlFeature.Returning;

    /// <inheritdoc />
    public string FeatureName => "RETURNING";

    /// <inheritdoc />
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