using System.Runtime.CompilerServices;
using System.Text;
using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlContext _sqlContext;
    private readonly List<object> _segments; // Holds strings, fragments, and parameters
    private ParseState _state;

    private static readonly SqlKeyword[] _initiatorsOrdered = SqlKeyword.AllKeywords
        .Where(k => k.IsClauseInitiator)
        .OrderByDescending(k => k.Value.Length)
        .ToArray();

    private struct ParseState
    {
        public SqlKeyword? CurrentKeyword;
        public bool IsInsideString;
        public ISqlProjection? LastProjection;
        public int ParameterCount;
    }

    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlContext context)
    {
        _sqlContext = context;
        _segments = new List<object>(literalLength / 10 + formattedCount);
        _state = new ParseState();
    }

    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        var span = value.AsSpan();
        
        // 1. Lexing to track current SQL Clause (SELECT, FROM, etc)
        ProcessLiteralSpan(span);

        // 2. Natural AS Capture
        // If the previous hole was a table, and this text starts with " AS alias"
        if (_state.LastProjection != null && TryCaptureAlias(span, out var alias))
        {
            _state.LastProjection.Reference.Alias = alias;
            _state.LastProjection = null; 
        }

        _segments.Add(value);
    }

    public void AppendFormatted(object? value)
    {
        _state.LastProjection = null;

        switch (value)
        {
            case ISqlProjection projection:
                _state.LastProjection = projection;
                // Store a "View" of this projection based on current context
                _segments.Add(new ProjectionView(projection, _state.CurrentKeyword));
                break;

            case ISqlReference reference:
                _segments.Add(reference);
                break;

            case ISqlFragment fragment:
                _segments.Add(fragment);
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
        
        // Store a marker for the parameter
        _segments.Add(new QueryParameter(paramKey));
    }

    public readonly string GetBuiltSql()
    {
        var sb = new StringBuilder();
        foreach (var segment in _segments)
        {
            var text = segment switch
            {
                string s => s,
                ProjectionView pv => pv.ToSql(_sqlContext),
                ISqlFragment frag => frag.ToSql(_sqlContext),
                QueryParameter p => $"{_sqlContext.Dialect.ParameterPrefix}{p.Key}",
                _ => segment?.ToString() ?? string.Empty
            };
            sb.Append(text);
        }
        return sb.ToString();
    }

    // --- Internal Helpers ---

    private void ProcessLiteralSpan(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == '\'' && (i == 0 || span[i-1] != '\\'))
                _state.IsInsideString = !_state.IsInsideString;

            if (_state.IsInsideString) continue;

            if (i == 0 || char.IsWhiteSpace(span[i - 1]))
                UpdateClauseIfMatch(span.Slice(i));
        }
    }

    private void UpdateClauseIfMatch(ReadOnlySpan<char> slice)
    {
        foreach (var keyword in _initiatorsOrdered)
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

    private bool TryCaptureAlias(ReadOnlySpan<char> span, out string? alias)
    {
        alias = null;
        var trimmed = span.TrimStart();
        if (trimmed.StartsWith(SqlKeyword.As.Value, StringComparison.OrdinalIgnoreCase))
        {
            var part = trimmed.Slice(SqlKeyword.As.Value.Length).TrimStart();
            int end = 0;
            while (end < part.Length && !char.IsWhiteSpace(part[end]) && part[end] != ',' && part[end] != ')' && part[end] != ';')
                end++;

            if (end > 0) { alias = part.Slice(0, end).ToString(); return true; }
        }
        return false;
    }

    // Helper records for the segment list
    private record ProjectionView(ISqlProjection Projection, SqlKeyword? Context) : ISqlFragment {
        public string ToSql(SqlContext ctx) => Context?.ExpectsDeclaration == true 
            ? Projection.Declaration.ToSql(ctx) 
            : Projection.Reference.ToSql(ctx);
    }
    private record QueryParameter(string Key);
}