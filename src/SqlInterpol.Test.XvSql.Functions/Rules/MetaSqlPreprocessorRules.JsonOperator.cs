using SqlInterpol.Parsing;

namespace SqlInterpol.Test.XvSql.Functions;

public partial class XvSqlPreprocessorRules
{
    private partial void InitializeJsonOperatorRule()
    {
        _rules.Add(ProcessJsonOperator);
    }

    private bool ProcessJsonOperator(ref SqlSegment segment, IReadOnlyList<SqlSegment> segments, int index, SqlPreprocessorState state)
    {
        if (segment.Type == SqlSegmentType.Literal && segment.Value is string text)
        {
            // Intercept and transpile the proprietary JSON operator
            if (text.Contains("->>"))
            {
                var newText = text.Replace("->>", " JSON_EXTRACT ");
                state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, newText, segment.RenderMode, segment.Tags));
                return true;
            }
        }
        
        return false;
    }
}