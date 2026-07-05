namespace SqlInterpol.Test.MetaSql.Functions;

public partial class MetaSqlFunctionsRewriter
{
    // Hook into the main constructor initialization loop
    private partial void InitializeConcatWsStrategy()
    {
        _strategies.Add("Meta_ConcatWS", TransformConcatWs);
    }

    private SqlSegment TransformConcatWs(SqlSegment seg, ISqlContext context)
    {
        if (context.Dialect.Kind == SqlDialectKind.SqLite)
        {
            return new SqlSegment(SqlSegmentType.Literal, "group_concat", seg.RenderMode, seg.Tags);
        }
        
        return seg;
    }
}