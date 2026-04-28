using System.Linq.Expressions;
using SqlInterpol.Config;
using SqlInterpol.Parsing;

namespace SqlInterpol.References;

public class SqlColumnReference(ISqlReference sourceReference, LambdaExpression expression) : SqlColumnBase(sourceReference)
{
    private readonly LambdaExpression _expression = expression;

    protected override string GetColumnName(SqlContext context) 
        => SqlExpressionHelper.GetMemberName(_expression);
}