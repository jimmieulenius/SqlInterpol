namespace SqlInterpol.Dialects.SqlServer;

/// <summary>
/// A helper fragment that renders an ANSI MERGE statement for SQL Server's upsert
/// (ON CONFLICT DO UPDATE SET) emulation.
/// </summary>
public class SqlServerMergeFragment(
    ISqlEntityBase targetTable,
    SqlInsertValuesFragment insertFragment,
    IReadOnlyList<ISqlProjection> conflictColumns,
    SqlSetFragment updateFragment) : ISqlFragment
{
    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var target = targetTable.ToSql(context);
        var insertCols = insertFragment.Assignments[0].Select(a => a.Reference.ToSql(context, SqlRenderMode.BaseName)).ToList();
        
        var sourceRows = new List<string>();

        foreach (var row in insertFragment.Assignments)
        {
            var vals = row.Select(a => a.ToSql(context).Split('=').Last().Trim());
            sourceRows.Add($"({string.Join(", ", vals)})");
        }
        
        var usingClause = $"USING (VALUES {string.Join(", ", sourceRows)}) AS source({string.Join(", ", insertCols)})";
        var onClause = string.Join(" AND ", conflictColumns.Select(c => $"target.{c.ToSql(context, SqlRenderMode.BaseName)} = source.{c.ToSql(context, SqlRenderMode.BaseName)}"));
        var updateSets = updateFragment.Assignments.Select(a => $"target.{a.Reference.ToSql(context, SqlRenderMode.BaseName)} = {a.ToSql(context).Split('=').Last().Trim()}");
        var insertVals = insertFragment.Assignments[0].Select(a => $"source.{a.Reference.ToSql(context, SqlRenderMode.BaseName)}");

        var nl = Environment.NewLine;
        return $"MERGE INTO {target} AS target{nl}{usingClause}{nl}ON {onClause}{nl}WHEN MATCHED THEN{nl}  UPDATE SET {string.Join(", ", updateSets)}{nl}WHEN NOT MATCHED THEN{nl}  INSERT ({string.Join(", ", insertCols)}){nl}  VALUES ({string.Join(", ", insertVals)});";
    }
}