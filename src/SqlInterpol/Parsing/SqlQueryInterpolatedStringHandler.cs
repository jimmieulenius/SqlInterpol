using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlContext _sqlContext;
    private Span<SqlSegment> _segments; // This will now point to a pooled array
    private int _segmentCount;
    private ParseState _state;
    private SqlSegment[] _arrayToReturn; // We always use the pool now

    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder, out bool shouldAppend)
    {
        _sqlContext = builder.Context;
        _state = new ParseState();
        
        // Estimate segments: literals + holes
        int estimatedSegments = (literalLength / 10) + formattedCount + 2;

        // Rent from the pool immediately. 
        // Even for small queries, the Pool is extremely efficient.
        _arrayToReturn = ArrayPool<SqlSegment>.Shared.Rent(Math.Max(estimatedSegments, 16));
        _segments = _arrayToReturn;

        _segmentCount = 0;
        shouldAppend = true;
    }

    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        AddSegment(new SqlSegment(SegmentType.Literal, value));

        var span = value.AsSpan();

        if (_sqlContext.PendingAliasCapture != null)
        {
            if (TryCaptureAlias(span, out var alias, out int consumed))
            {
                _sqlContext.PendingAliasCapture.Reference.Alias = alias;
                _sqlContext.PendingAliasCapture = null;
                span = span.Slice(consumed);
            }
            else if (IsCaptureTerminated(span))
            {
                _sqlContext.PendingAliasCapture = null;
            }
        }

        ProcessLiteralSpan(span);
    }

    public void AppendFormatted(object? value)
    {
        _state.LastProjection = null;

        switch (value)
        {
            case ISqlProjection projection:
                _state.LastProjection = projection;
                _sqlContext.PendingAliasCapture = projection; 
                AddSegment(new SqlSegment(SegmentType.Projection, projection, _state.CurrentKeyword));
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
        string paramKey = $"p{_state.ParameterCount++}";
        _sqlContext.Parameters[paramKey] = value ?? DBNull.Value;
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
        _segments.Slice(0, _segmentCount).CopyTo(newArray);
        
        // Return the old array to the pool
        ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
        _arrayToReturn = newArray;
        _segments = _arrayToReturn;
    }

    public string GetBuiltSql()
    {
        // ValueStringBuilder can STILL use stackalloc because 'char' is unmanaged!
        var vsb = new ValueStringBuilder(stackalloc char[512]);
        try
        {
            for (int i = 0; i < _segmentCount; i++)
            {
                ref var segment = ref _segments[i];
                switch (segment.Type)
                {
                    case SegmentType.Literal:
                        vsb.Append((string)segment.Value!);
                        break;
                    case SegmentType.Projection:
                        var proj = (ISqlProjection)segment.Value!;
                        var sql = segment.Context?.ExpectsDeclaration == true 
                            ? proj.Declaration.ToSql(_sqlContext) 
                            : proj.Reference.ToSql(_sqlContext);
                        vsb.Append(sql);
                        break;
                    case SegmentType.Reference:
                        vsb.Append(((ISqlReference)segment.Value!).ToSql(_sqlContext));
                        break;
                    case SegmentType.Fragment:
                        vsb.Append(((ISqlFragment)segment.Value!).ToSql(_sqlContext));
                        break;
                    case SegmentType.Parameter:
                        vsb.Append(_sqlContext.Dialect.ParameterPrefix);
                        vsb.Append((string)segment.Value!);
                        break;
                }
            }
            return vsb.ToString();
        }
        finally
        {
            vsb.Dispose();
            // Crucial: Return the segment array to the pool when finished
            ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
        }
    }

    // --- Internal State & Lexing (Based on your provided logic) ---

    private struct ParseState
    {
        public SqlKeyword? CurrentKeyword;
        public bool IsInsideString;
        public ISqlProjection? LastProjection;
        public int ParameterCount;
    }

    internal enum SegmentType { Literal, Projection, Reference, Fragment, Parameter }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct SqlSegment
    {
        public readonly SegmentType Type;
        public readonly object? Value;
        public readonly SqlKeyword? Context;

        public SqlSegment(SegmentType type, object? value, SqlKeyword? context = null)
        {
            Type = type;
            Value = value;
            Context = context;
        }
    }

    private void ProcessLiteralSpan(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            var slice = span.Slice(i);
            if (span[i] == '\'' && (i == 0 || span[i - 1] != '\\'))
            {
                _state.IsInsideString = !_state.IsInsideString;
                continue;
            }
            if (_state.IsInsideString) continue;

            if (slice.StartsWith("--"))
            {
                int nl = slice.IndexOfAny('\r', '\n');
                i += (nl == -1) ? slice.Length : nl;
                continue;
            }
            if (slice.StartsWith("/*"))
            {
                int end = slice.Slice(2).IndexOf("*/");
                i += (end == -1) ? slice.Length : end + 3;
                continue;
            }

            if (i == 0 || char.IsWhiteSpace(span[i - 1]))
                UpdateClauseIfMatch(slice);
        }
    }

    private void UpdateClauseIfMatch(ReadOnlySpan<char> slice)
    {
        // Optimized check against ordered initiators (SELECT, FROM, etc.)
        foreach (var keyword in SqlKeyword.AllInitiatorsOrdered)
        {
            if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
            {
                if (slice.Length == keyword.Value.Length || !char.IsLetterOrDigit(slice[keyword.Value.Length]))
                {
                    _state.CurrentKeyword = keyword;
                    return;
                }
            }
        }
    }

    private bool IsCaptureTerminated(ReadOnlySpan<char> span)
    {
        var current = span;
        if (!SkipWhitespaceAndComments(ref current)) return false;

        // If the next character is a comma, closing paren, or semicolon, 
        // it's impossible for a table alias to follow.
        char c = current[0];
        return c == ',' || c == ')' || c == ';' || c == '(';
    }

    private bool TryCaptureAlias(ReadOnlySpan<char> span, out string? alias, out int consumed)
    {
        alias = null;
        consumed = 0;
        var current = span;

        if (!SkipWhitespaceAndComments(ref current)) return false;

        // 1. Handle explicit 'AS'
        bool hasExplicitAs = false;
        if (current.StartsWith("AS", StringComparison.OrdinalIgnoreCase))
        {
            // Ensure word boundary (AS vs ASSET)
            if (current.Length == 2 || !char.IsLetterOrDigit(current[2]))
            {
                hasExplicitAs = true;
                current = current.Slice(2);
                if (!SkipWhitespaceAndComments(ref current)) return false;
            }
        }

        // 2. Identify the potential alias token
        int end = 0;
        while (end < current.Length && (char.IsLetterOrDigit(current[end]) || current[end] == '_'))
        {
            end++;
        }

        if (end > 0)
        {
            var token = current.Slice(0, end).ToString();

            // If it's a SQL keyword (like WHERE, JOIN), it's not an alias
            if (IsSqlKeyword(token)) return false;

            alias = token;
            // Calculate total characters consumed from the original span
            consumed = span.Length - current.Slice(end).Length;
            return true;
        }

        return false;
    }

    private static bool SkipWhitespaceAndComments(ref ReadOnlySpan<char> span)
    {
        while (span.Length > 0)
        {
            if (char.IsWhiteSpace(span[0])) { span = span.Slice(1); continue; }
            if (span.StartsWith("--")) { /* skip line */ }
            if (span.StartsWith("/*")) { /* skip block */ }
            break; 
        }
        return span.Length > 0;
    }

    private static bool IsSqlKeyword(string word) => 
        SqlKeyword.AllKeywords.Any(k => k.Value.Equals(word, StringComparison.OrdinalIgnoreCase));
}