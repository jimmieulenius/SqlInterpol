using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlInsertFragment(ISqlEntityBase entity, IEnumerable<ISqlAssignmentFragment> assignments) 
    : ISqlFragment, ISqlParameterGenerator
{
    public void GenerateParameters(ISqlContext context)
    {
        // Ensure values reserve their @p0, @p1 indices in order
        foreach (var assignment in assignments)
        {
            if (assignment is ISqlParameterGenerator generator)
                generator.GenerateParameters(context);
        }
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // 1. Render the "INSERT INTO [Table]" part
        string intoSql = $"{SqlKeyword.Insert} {SqlKeyword.Into} {entity.Declaration.ToSql(context)}";

        // 2. Render the "(Cols...) VALUES (Vals...)" part
        // Note: We create the Values fragment here to handle the split logic
        var valuesFragment = new SqlInsertValuesFragment(assignments);
        
        return $"{intoSql}{Environment.NewLine}{valuesFragment.ToSql(context)}";
    }
}