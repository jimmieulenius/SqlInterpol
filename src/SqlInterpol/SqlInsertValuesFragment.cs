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

        var columnNames = new SqlCollectionFragment([.. assignmentList.Select(a => new SqlRawFragment(a.Reference.ToSql(context, SqlRenderMode.BaseName)))]);
        var paramNames = new SqlCollectionFragment([.. assignmentList.Select(a => new SqlRawFragment(a.ToSql(context).Split('=').Last().Trim()))]);
        var cols = columnNames.ToSql(context, mode);
        var vals = paramNames.ToSql(context, mode);

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            return $"({cols}{Environment.NewLine}){Environment.NewLine}{SqlKeyword.Values}{Environment.NewLine}({vals}{Environment.NewLine})";
        }

        return $"({cols}){Environment.NewLine}{SqlKeyword.Values} ({vals})";
    }
}