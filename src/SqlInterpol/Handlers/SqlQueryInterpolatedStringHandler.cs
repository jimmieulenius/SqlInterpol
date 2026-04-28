using System.Runtime.CompilerServices;
using System.Text;
using SqlInterpol.Abstractions;
using SqlInterpol.Models;

namespace SqlInterpol.Handlers;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly StringBuilder _builder;
    private readonly SqlContext _sqlContext;
    private ParseState _state;

    // "Longest Match Wins" - ensures "INNER JOIN" is matched before "JOIN"
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
        // Pre-allocate buffer to reduce resizing
        _builder = new StringBuilder(literalLength + (formattedCount * 16));
        _state = new ParseState();
    }

    /// <summary>
    /// Processes standard SQL text between holes.
    /// Handles Keyword detection, String Literal escaping, and Alias capture.
    /// </summary>
    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        var span = value.AsSpan();
        
        // 1. Lexing: Update Clause context and check for string literal boundaries
        ProcessLiteralSpan(span);

        // 2. Natural AS Capture:
        // If we just appended a table/view, check if this literal starts with " AS "
        if (_state.LastProjection != null && TryCaptureAlias(span, out var alias))
        {
            _state.LastProjection.Reference.Alias = alias;
            _state.LastProjection = null; 
        }

        _builder.Append(value);
    }

    /// <summary>
    /// Processes objects passed in {holes}.
    /// </summary>
    public void AppendFormatted(object? value)
    {
        // Reset last projection at start of every formatted item
        _state.LastProjection = null;

        switch (value)
        {
            case ISqlProjection projection:
                _state.LastProjection = projection;
                
                // The current keyword metadata dictates whether we render the 
                // "Declaration" (Source + AS) or the "Reference" (Alias/Name only).
                var sql = (_state.CurrentKeyword?.ExpectsDeclaration == true)
                    ? projection.Declaration.ToSql(_sqlContext)
                    : projection.Reference.ToSql(_sqlContext);
                
                _builder.Append(sql);
                break;

            case ISqlReference reference:
                _builder.Append(reference.ToSql(_sqlContext));
                break;

            case ISqlFragment fragment:
                _builder.Append(fragment.ToSql(_sqlContext));
                break;

            default:
                // Treat everything else as a Query Parameter
                HandleParameter(value);
                break;
        }
    }

    private void ProcessLiteralSpan(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            // Handle string literal boundaries and escaping ('' or \')
            if (c == '\'' && !IsEscaped(span, i))
            {
                _state.IsInsideString = !_state.IsInsideString;
                continue;
            }

            // Skip keyword detection if we are inside a SQL string literal '...'
            if (_state.IsInsideString) continue;

            // Keyword detection: only check at the start of the string or following whitespace
            if (i == 0 || char.IsWhiteSpace(span[i - 1]))
            {
                UpdateClauseIfMatch(span.Slice(i));
            }
        }
    }

    private void UpdateClauseIfMatch(ReadOnlySpan<char> slice)
    {
        foreach (var keyword in _initiatorsOrdered)
        {
            if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
            {
                // Ensure it's a whole word match (not a prefix of a column name)
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
            var aliasPart = trimmed.Slice(SqlKeyword.As.Value.Length).TrimStart();
            int end = 0;
            
            // Delimiters that end an alias: whitespace, comma, semicolon, or closing paren
            while (end < aliasPart.Length && !char.IsWhiteSpace(aliasPart[end]) && 
                   aliasPart[end] != ',' && aliasPart[end] != ';' && aliasPart[end] != ')')
            {
                end++;
            }

            if (end > 0)
            {
                alias = aliasPart.Slice(0, end).ToString();
                return true;
            }
        }
        return false;
    }

    private bool IsEscaped(ReadOnlySpan<char> span, int index)
    {
        // SQL Server standard: ''
        if (index < span.Length - 1 && span[index + 1] == '\'') return true;
        // C-Style: \'
        if (index > 0 && span[index - 1] == '\\') return true;
        
        return false;
    }

    private void HandleParameter(object? value)
    {
        // 1. Generate unique key for Dapper dictionary
        string paramKey = $"p{_state.ParameterCount++}";
        
        // 2. Store the value in the SqlContext's dictionary
        // We use the context to ensure the dictionary persists after the handler is disposed
        _sqlContext.Parameters[paramKey] = value ?? DBNull.Value;

        // 3. Append the dialect-specific parameter token (e.g. @p0)
        _builder.Append(_sqlContext.Dialect.ParameterPrefix);
        _builder.Append(paramKey);
    }

    public string GetBuiltSql() => _builder.ToString();
}