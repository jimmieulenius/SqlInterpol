namespace SqlInterpol.Dialects.SqlServer;

/// <summary>
/// A helper fragment that wraps <see cref="SqlInsertValuesFragment"/> to inject an
/// <c>OUTPUT inserted.*</c> clause for SQL Server's RETURNING emulation.
/// </summary>
public class SqlServerInsertValuesFragment(SqlInsertValuesFragment original, IReadOnlyList<ISqlProjection> returnedColumns) : ISqlFragment, ISqlParameterGenerator
{
    /// <inheritdoc />
    public void GenerateParameters(ISqlContext context) => original.GenerateParameters(context);

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var baseSql = original.ToSql(context, mode);
        var outputCols = string.Join(", ", returnedColumns.Select(c => $"inserted.{c.ToSql(context, SqlRenderMode.BaseName)}"));
        return baseSql.Replace(SqlKeyword.Values, $"OUTPUT {outputCols}{Environment.NewLine}{SqlKeyword.Values}");
    }
}