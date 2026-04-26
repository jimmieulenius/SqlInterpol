namespace SqlInterpol.Models;

public class SqlTableJoinPending : SqlTableJoin
{
    private readonly string _joinType;
    private readonly SqlReference _otherTable;

    internal SqlTableJoinPending(SqlTableJoin accumulator, string joinType, SqlReference otherTable)
        : base(accumulator)
    {
        _joinType = joinType;
        _otherTable = otherTable;
    }

    public SqlTableJoin On(SqlColumn leftColumn, SqlColumn rightColumn)
    {
        AddJoin(_joinType, _otherTable, leftColumn, rightColumn);

        // Return as SqlTableJoin so user can continue chaining
        return this;
    }
}