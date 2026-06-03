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

    /// <summary>
    /// Appends an interpolated value as a processed segment, capturing the C# expression string
    /// to automatically map lambda variables to SQL entities and columns without string allocations.
    /// </summary>
    public void AppendFormatted<T>(T value, string? format = null, [CallerArgumentExpression("value")] string? expression = null)
    {
        // 1. PRESERVE OLD SYNTAX: Process explicit SQL fragments normally
        if (value is ISqlFragment frag)
        {
            AddSegment(_builder.ProcessValue(frag));
            return;
        }

        // 2. NEW SYNTAX: AST Routing via CallerArgumentExpression
        if (!string.IsNullOrEmpty(expression))
        {
            int dotIndex = expression.IndexOf('.');

            // Scenario A: Direct POCO access (e.g., FROM {p} or {stats:decl})
            if (dotIndex == -1)
            {
                if (_builder.ScopedVariables.TryGetValue(expression, out var tableEntity))
                {
                    SqlRenderMode? mode = format switch
                    {
                        "decl"  => SqlRenderMode.Declaration,
                        "alias" => SqlRenderMode.AliasOnly,
                        "base"  => SqlRenderMode.BaseName,
                        _       => null
                    };

                    AddSegment(new SqlSegment(SqlSegmentType.Reference, tableEntity, mode));
                    return;
                }
            }
            // Scenario B: Column projection (e.g., SELECT {p.Id} or {stats.TotalPrice:col})
            else if (dotIndex > 0 && expression.LastIndexOf('.') == dotIndex)
            {
                var varName = expression[..dotIndex];
                var propertyName = expression[(dotIndex + 1)..];

                if (_builder.ScopedVariables.TryGetValue(varName, out var entity))
                {
                    var entityModelType = entity.GetType().GetGenericArguments()[0];
                    var meta = SqlMetadataRegistry.GetMetadata(entityModelType);
                    
                    var columnMap = meta.Columns.FirstOrDefault(c => c.Key.Name == propertyName);
                    string physicalColumnName = columnMap.Key != null ? columnMap.Value : propertyName;

                    var columnRef = new SqlColumnReference(entity.Reference, physicalColumnName, propertyName);
                    
                    SqlRenderMode? mode = format switch
                    {
                        "col"   => SqlRenderMode.BaseName,
                        "alias" => SqlRenderMode.AliasOnly,
                        _       => null
                    };

                    AddSegment(new SqlSegment(SqlSegmentType.Projection, columnRef, mode));
                    return;
                }
            }
        }

        // 3. Fallback for standard parameters and iterables (Parser handles SqlCollectionFragment)
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