
namespace SqlInterpol;

/// <summary>
/// Renders a single-table UPDATE statement for the given entity,
/// generating parameters and emitting <c>UPDATE [table] SET col = @pN, ...</c>.
/// </summary>
/// <param name="entity">The entity whose table is the UPDATE target.</param>
/// <param name="assignments">The column-value assignments for the SET clause.</param>
public class SqlUpdateFragment(ISqlEntityBase entity, IEnumerable<ISqlAssignmentFragment> assignments) 
    : ISqlFragment, ISqlParameterGenerator
{
    /// <inheritdoc />
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

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var separator = context.Options.CollectionSeparator;
        string updateSql = $"{SqlKeyword.Update} {entity.Declaration.ToSql(context)}";
        
        var list = assignments.Select(a => a.ToSql(context)).ToList();
        string setClause;

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            separator = separator.TrimEnd();
            var indent = new string(' ', context.Options.IndentSize);
            setClause = $"{Environment.NewLine}{indent}{string.Join($"{separator}{Environment.NewLine}{indent}", list)}";
        }
        else
        {
            setClause = $" {string.Join(separator, list)}";
        }
        
        return $"{updateSql}{Environment.NewLine}{SqlKeyword.Set}{setClause}";
    }
}