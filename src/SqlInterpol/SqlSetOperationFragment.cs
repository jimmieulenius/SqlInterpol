using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlSetOperationFragment : ISqlFragment
{
    public ISqlFragment Left { get; }
    public ISqlFragment Right { get; }
    public SqlSetOperator Operator { get; }

    public SqlSetOperationFragment(ISqlFragment left, ISqlFragment right, SqlSetOperator setOperator)
    {
        Left = left;
        Right = right;
        Operator = setOperator;
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        // return $"{Left.ToSql(context)}{Environment.NewLine}{Operator.ToString().ToUpper()}{Environment.NewLine}{Right.ToSql(context)}";
        return context.Dialect.RenderFragment(this, context);
    }
}