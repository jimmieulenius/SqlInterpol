using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// Internal implementation of the pipeline state tracker.
/// </summary>
internal class SqlPipelineState : ISqlPipelineState
{
    private readonly HashSet<string> _tags;

    public ISqlContext Context { get; }

    public SqlPipelineState(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        Context = context;
        
        // Initialize with default capacity, case-insensitive
        _tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < segments.Count; i++)
        {
            var tags = segments[i].Tags;
            if (tags != null)
            {
                for (int j = 0; j < tags.Length; j++)
                {
                    _tags.Add(tags[j]);
                }
            }
        }
    }

    public bool HasTag(string tag) => _tags.Contains(tag);
}