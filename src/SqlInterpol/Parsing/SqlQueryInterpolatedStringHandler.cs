using System.Buffers;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlBuilder _builder;
    private Span<SqlSegment> _segments;
    private int _segmentCount;
    private SqlSegment[] _arrayToReturn;

    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder, out bool shouldAppend)
    {
        _builder = builder;
        shouldAppend = true;

        int estimated = (literalLength / 10) + formattedCount + 2;
        _arrayToReturn = ArrayPool<SqlSegment>.Shared.Rent(Math.Max(estimated, 16));
        _segments = _arrayToReturn;
        _segmentCount = 0;
    }

    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        AddSegment(_builder.ProcessLiteral(value));
    }

    public void AppendFormatted(object? value)
    {
        AddSegment(_builder.ProcessValue(value));
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
        if (_arrayToReturn != null) ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
        _arrayToReturn = newArray;
        _segments = _arrayToReturn;
    }

    internal void TransferSegments(List<SqlSegment> destination)
    {
        for (int i = 0; i < _segmentCount; i++) destination.Add(_segments[i]);
        if (_arrayToReturn != null)
        {
            ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
            _arrayToReturn = null!;
        }
    }
}