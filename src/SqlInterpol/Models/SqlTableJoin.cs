using System.Text;
using SqlInterpol.Constants;

namespace SqlInterpol.Models;

public class SqlTableJoin : SqlReference
{
    protected record JoinInfo(string JoinType, SqlReference Table, SqlColumn? LeftColumn, SqlColumn? RightColumn, List<(Sql condition, string op)> AdditionalConditions);
    protected readonly SqlReference _baseTable;
    protected readonly List<JoinInfo> _joins = new();

    public SqlTableJoin(SqlReference baseTable)
        : base(baseTable.Name, baseTable.Alias())
    {
        _baseTable = baseTable;
    }

    internal SqlTableJoin(SqlTableJoin accumulator)
        : base(accumulator.Name, accumulator.Alias())
    {
        _baseTable = accumulator._baseTable;

        foreach (var join in accumulator._joins)
        {
            _joins.Add(join);
        }
    }

    public SqlTableJoinPending InnerJoin(SqlReference otherTable) => new SqlTableJoinPending(this, SqlKeyword.InnerJoin, otherTable);

    public SqlTableJoinPending LeftJoin(SqlReference otherTable) => new SqlTableJoinPending(this, SqlKeyword.LeftJoin, otherTable);

    public SqlTableJoinPending RightJoin(SqlReference otherTable) => new SqlTableJoinPending(this, SqlKeyword.RightJoin, otherTable);

    public SqlTableJoinPending FullOuterJoin(SqlReference otherTable) => new SqlTableJoinPending(this, SqlKeyword.FullOuterJoin, otherTable);

    public SqlTableJoin CrossJoin(SqlReference otherTable)
    {
        _joins.Add(new JoinInfo(SqlKeyword.CrossJoin, otherTable, null, null, []));

        return this;
    }

    internal SqlTableJoin AddJoin(string joinType, SqlReference table, SqlColumn leftColumn, SqlColumn rightColumn)
    {
        _joins.Add(new JoinInfo(joinType, table, leftColumn, rightColumn, []));

        return this;
    }

    internal SqlTableJoin AddConditionToLastJoin(Sql condition, string op = SqlKeyword.And)
    {
        if (_joins.Count > 0)
        {
            var lastJoin = _joins[_joins.Count - 1];
            lastJoin.AdditionalConditions.Add((condition, op));
        }

        return this;
    }

    public SqlTableJoin And(Sql condition)
    {
        AddConditionToLastJoin(condition, SqlKeyword.And);

        return this;
    }

    public SqlTableJoin Or(Sql condition)
    {
        AddConditionToLastJoin(condition, SqlKeyword.Or);

        return this;
    }

    internal IEnumerable<Sql> GetAdditionalConditions()
    {
        foreach (var join in _joins)
        {
            foreach (var (condition, op) in join.AdditionalConditions)
            {
                yield return condition;
            }
        }
    }

    public override string FullName => _baseTable.Reference;

    public override string Reference => _baseTable.Reference;

    public override string ToString(string clause, SqlInterpolOptions options)
    {
        var result = new StringBuilder();
        var indent = new string(' ', options.IndentSize);
        result.Append(_baseTable.ToString(clause, options));

        foreach (var join in _joins)
        {
            result.Append(Environment.NewLine);
            result.Append(join.JoinType);
            result.Append(" ");
            result.Append(join.Table.ToString(clause, options));

            if (join.LeftColumn != null && join.RightColumn != null)
            {
                result.Append(Environment.NewLine);
                result.Append(indent);
                result.Append(SqlKeyword.On);
                result.Append(" ");
                // Pass SqlKeyword.On as clause context so columns render without SELECT aliases
                result.Append(join.LeftColumn.ToString(SqlKeyword.On, options));
                result.Append(" = ");
                result.Append(join.RightColumn.ToString(SqlKeyword.On, options));
                
                // Add any additional AND/OR conditions
                foreach (var (condition, op) in join.AdditionalConditions)
                {
                    result.Append(Environment.NewLine);
                    result.Append(indent);
                    result.Append(op);
                    result.Append(" ");
                    result.Append(condition.ToString());
                }
            }
        }

        return result.ToString();
    }

    public override SqlReference As(string alias)
    {
        _alias = alias;

        return this;
    }
}