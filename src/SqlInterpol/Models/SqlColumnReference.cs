using System.Linq.Expressions;
using SqlInterpol.Abstractions;
using SqlInterpol.Helpers;

namespace SqlInterpol.Models;

public class SqlColumnReference(ISqlReference sourceReference, LambdaExpression expression) : SqlColumnBase(sourceReference)
{
    private readonly LambdaExpression _expression = expression;

    protected override string GetColumnName(SqlContext context) 
        => SqlExpressionHelper.GetMemberName(_expression);
}