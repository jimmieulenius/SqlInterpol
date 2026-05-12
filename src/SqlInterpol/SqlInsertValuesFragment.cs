using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlInsertValuesFragment(IEnumerable<ISqlAssignmentFragment> assignments) 
    : ISqlFragment, ISqlParameterGenerator
{
    private readonly List<ISqlAssignmentFragment> _assignments = assignments.ToList();

    public void GenerateParameters(ISqlContext context)
    {
        foreach (var assignment in _assignments)
        {
            if (assignment is ISqlParameterGenerator generator)
            {
                generator.GenerateParameters(context);
            }
        }
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var assignmentList = assignments.ToList();
        var separator = context.Options.CollectionSeparator;

        if (assignmentList.Count == 0)
        {
            return string.Empty;
        }

        var columnNames = assignmentList.Select(a => a.Reference.ToSql(context, SqlRenderMode.BaseName)).ToList();
        var paramNames = assignmentList.Select(a => a.ToSql(context).Split('=').Last().Trim()).ToList();

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            var indent = new string(' ', context.Options.IndentSize);
            var cols = FormatVertical(columnNames, context);
            var vals = FormatVertical(paramNames, context);

            return $"({cols}{Environment.NewLine}){Environment.NewLine}{SqlKeyword.Values}{Environment.NewLine}({vals}{Environment.NewLine})";
        }

        return $"({string.Join(separator, columnNames)}){Environment.NewLine}{SqlKeyword.Values} ({string.Join(separator, paramNames)})";
    }

    private static string FormatVertical(List<string> items, ISqlContext context)
    {
        var indent = new string(' ', context.Options.IndentSize);
        var separator = context.Options.CollectionSeparator.TrimEnd();

        return $"{Environment.NewLine}{indent}{string.Join($"{separator}{Environment.NewLine}{indent}", items)}";
    }
}