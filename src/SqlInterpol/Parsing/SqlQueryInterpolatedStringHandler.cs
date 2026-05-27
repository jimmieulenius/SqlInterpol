using System.Buffers;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

[InterpolatedStringHandler]
/// <summary>
/// The interpolated string handler for <see cref="SqlBuilder"/> that collects raw SQL literals
/// and interpolated values into a pooled segment buffer during query construction.
/// </summary>
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlBuilder _builder;
    private Span<SqlSegment> _segments;
    private int _segmentCount;
    private SqlSegment[] _arrayToReturn;

    /// <summary>
    /// Initializes the handler with a pooled segment buffer sized to the estimated number of segments.
    /// </summary>
    /// <param name="literalLength">Estimated total length of raw SQL literal characters (used for sizing).</param>
    /// <param name="formattedCount">Number of interpolated expression holes in the string.</param>
    /// <param name="builder">The <see cref="SqlBuilder"/> that owns this handler.</param>
    /// <param name="shouldAppend">Always set to <see langword="true"/>.</param>
    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder, out bool shouldAppend)
    {
        _builder = builder;
        shouldAppend = true;

        int estimated = (literalLength / 10) + formattedCount + 2;
        _arrayToReturn = ArrayPool<SqlSegment>.Shared.Rent(Math.Max(estimated, 16));
        _segments = _arrayToReturn;
        _segmentCount = 0;
    }

    /// <summary>Appends a raw SQL literal string as a processed segment.</summary>
    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        AddSegment(_builder.ProcessLiteral(value));
    }

    /// <summary>Appends an interpolated value as a processed segment.</summary>
    public void AppendFormatted(object? value)
    {
        AddSegment(_builder.ProcessValue(value));
    }

    // /// <summary>
    // /// Natively unwraps a captured query scope (sub-query) and injects its segments directly into the current stream.
    // /// </summary>
    // public void AppendFormatted(ISqlQuery query)
    // {
    //     if (query == null) return;

    //     var subSegments = query.Segments;
    //     for (int i = 0; i < subSegments.Count; i++)
    //     {
    //         AddSegment(subSegments[i]);
    //     }
    // }

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

    /// <summary>Transfers all collected segments into <paramref name="destination"/> and releases the pooled buffer.</summary>
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