using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlUpdateFragment(ISqlEntityBase entity, IEnumerable<ISqlAssignmentFragment> assignments) 
    : ISqlFragment, ISqlParameterGenerator
{
    public void GenerateParameters(ISqlContext context)
    {
        foreach (var assignment in assignments)
        {
            if (assignment is ISqlParameterGenerator generator)
            {
                generator.GenerateParameters(context);
            }
        }
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        string updateSql = $"{SqlKeyword.Update} {entity.Declaration.ToSql(context)}";
        string setClause = string.Join(", ", assignments.Select(a => a.ToSql(context)));

        return $"{updateSql}{Environment.NewLine}{SqlKeyword.Set} {setClause}";
    }
}