namespace SqlInterpol.Test.XvSql.Functions;

public partial class XvSqlFunctionsRewriter
{
    private partial void InitializeCastStrategy()
    {
        _strategies.Add("Meta_Cast", TransformCast);
    }

    private SqlSegment TransformCast(SqlSegment seg, ISqlContext context)
    {
        // Example: If a database requires specific casting keywords, handle it here!
        return seg;
    }
}