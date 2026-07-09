namespace SqlInterpol;

/// <summary>
/// The primary entry point for building parameterized, dialect-aware SQL queries using C# interpolated strings.
/// </summary>
public partial class SqlBuilder : ISqlEntityRegistry, ISqlGeneratorBuilder
{
    void ISqlGeneratorBuilder.AppendRaw(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _segments.Add(new SqlSegment(SqlSegmentType.Literal, text));
        }
    }

    void ISqlGeneratorBuilder.AppendNode(ISqlFragment node)
    {
        _segments.Add(new SqlSegment(SqlSegmentType.Raw, node));
    }

    void ISqlGeneratorBuilder.AppendTemplate(ISqlTemplate template)
    {
        _segments.Add(new SqlSegment(SqlSegmentType.Raw, template));
    }

    void ISqlGeneratorBuilder.AppendSegment(SqlSegment segment)
    {
        _segments.Add(segment);
    }
}