using SqlInterpol.Configuration;
using SqlInterpol.Infrastructure;

namespace SqlInterpol.Segments;

/// <summary>
/// Renders the <c>(col1, col2, ...) VALUES (val1, val2, ...)</c> clause
/// for single- or multi-row INSERT statements.
/// </summary>
public class SqlInsertValuesFragment : ISqlFragment, ISqlParameterGenerator
{
    private readonly List<List<ISqlAssignmentFragment>> _bulkAssignments;

    /// <summary>Gets all rows of column-value assignments, one inner list per row.</summary>
    public IReadOnlyList<IReadOnlyList<ISqlAssignmentFragment>> Assignments => _bulkAssignments;

    /// <summary>
    /// Initializes a single-row INSERT values fragment.
    /// </summary>
    /// <param name="assignments">The column-value assignments for the single row.</param>
    public SqlInsertValuesFragment(IEnumerable<ISqlAssignmentFragment> assignments)
    {
        _bulkAssignments = [[.. assignments]];
    }

    /// <summary>
    /// Initializes a multi-row (bulk) INSERT values fragment.
    /// </summary>
    /// <param name="bulkAssignments">A sequence of per-row column-value assignment lists.</param>
    public SqlInsertValuesFragment(IEnumerable<IEnumerable<ISqlAssignmentFragment>> bulkAssignments)
    {
        _bulkAssignments = [.. bulkAssignments.Select(a => a.ToList())];
    }

    /// <inheritdoc />
    public void GenerateParameters(ISqlContext context)
    {
        foreach (var row in _bulkAssignments)
        {
            foreach (var assignment in row)
            {
                if (assignment is ISqlParameterGenerator generator)
                {
                    generator.GenerateParameters(context);
                }
            }
        }
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (_bulkAssignments.Count == 0 || _bulkAssignments[0].Count == 0) return string.Empty;

        var firstRow = _bulkAssignments[0];
        var columnNames = new SqlCollectionFragment([.. firstRow.Select(a => new SqlRawFragment(a.Reference.ToSql(context, SqlRenderMode.BaseName)))]);
        var separator = context.Options.CollectionSeparator;
        var valuesBlocks = new List<string>();

        foreach (var row in _bulkAssignments)
        {
            var paramNames = new SqlCollectionFragment([.. row.Select(a => new SqlRawFragment(a.ToSql(context).Split('=').Last().Trim()))]);
            valuesBlocks.Add($"{paramNames.ToSql(context, mode)}");
        }

        var cols = columnNames.ToSql(context, mode);

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            separator = separator.TrimEnd();
            var vals = string.Join($"{separator}{Environment.NewLine}", valuesBlocks.Select(v => $"({v}{Environment.NewLine})"));
            return $"({cols}{Environment.NewLine}){Environment.NewLine}{SqlKeyword.Values}{Environment.NewLine}{vals}";
        }

        return $"({cols}){Environment.NewLine}{SqlKeyword.Values} {string.Join(separator, valuesBlocks.Select(v => $"({v})"))}";
    }
}