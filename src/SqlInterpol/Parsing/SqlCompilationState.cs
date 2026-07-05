namespace SqlInterpol.Parsing;

/// <summary>
/// Internal implementation of the compilation state tracker.
/// </summary>
internal class SqlCompilationState : ISqlCompilationState
{
    private readonly HashSet<string> _tags;

    public ISqlContext Context { get; }

    public SqlCompilationState(IReadOnlyList<SqlSegment> segments, ISqlContext context)
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