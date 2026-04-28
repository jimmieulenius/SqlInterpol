using System.Buffers;
using System.Runtime.CompilerServices;
using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlContext _context;
    private Span<SqlSegment> _segments;
    private int _segmentCount;
    private SqlSegment[] _arrayToReturn;

    public int SegmentCount => _segmentCount;

    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder, out bool shouldAppend)
    {
        _context = builder.Context;
        shouldAppend = true;

        int estimated = (literalLength / 10) + formattedCount + 2;
        _arrayToReturn = ArrayPool<SqlSegment>.Shared.Rent(Math.Max(estimated, 16));
        _segments = _arrayToReturn;
        _segmentCount = 0;
    }

    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        // 1. Analyze for metadata using the shared state in _context
        SqlParser.ProcessLiteral(_context, value.AsSpan());

        // 2. Add as a literal segment
        AddSegment(new SqlSegment(SegmentType.Literal, value));
    }

    public void AppendFormatted(object? value)
    {
        switch (value)
        {
            case ISqlProjection projection:
                _context.State.PendingAliasCapture = projection; 
                AddSegment(new SqlSegment(SegmentType.Projection, projection, _context.State.CurrentKeyword));
                break;
                
            case ISqlReference reference:
                AddSegment(new SqlSegment(SegmentType.Reference, reference));
                break;
                
            case ISqlFragment fragment:
                AddSegment(new SqlSegment(SegmentType.Fragment, fragment));
                break;
                
            default:
                HandleParameter(value);
                break;
        }
    }

    private void HandleParameter(object? value)
    {
        // Use the persistent parameter count from the context
        string paramKey = $"p{_context.State.ParameterCount++}";
        _context.Parameters[paramKey] = value ?? DBNull.Value;
        AddSegment(new SqlSegment(SegmentType.Parameter, paramKey));
    }

    private void AddSegment(SqlSegment segment)
    {
        if (_segmentCount >= _segments.Length) GrowBuffer();
        _segments[_segmentCount++] = segment;
    }

    private void GrowBuffer()
    {
        int newSize = _segments.Length * 2;
        var newArray = ArrayPool<SqlSegment>.Shared.Rent(newSize);
        _segments[.._segmentCount].CopyTo(newArray);
        
        ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
        _arrayToReturn = newArray;
        _segments = _arrayToReturn;
    }

    internal void TransferSegments(List<SqlSegment> destination)
    {
        for (int i = 0; i < _segmentCount; i++)
            destination.Add(_segments[i]);

        if (_arrayToReturn != null)
        {
            ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
            _arrayToReturn = null!;
        }
    }
}