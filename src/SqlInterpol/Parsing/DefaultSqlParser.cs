using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

public class DefaultSqlParser : ISqlParser
{
    public virtual SqlSegment ProcessValue(SqlContext context, object? value)
    {
        bool isAlias = context.ParseState.ExpectsAliasOnly;
        context.ParseState.ExpectsAliasOnly = false; // consume immediately

        if (value is ISqlProjection projection)
        {
            if (!isAlias)
            {
                context.ParseState.LastAliasableTarget = projection;
            }

            return new SqlSegment(SqlSegmentType.Projection, projection, isAliasTarget: isAlias);
        }

        if (value is ISqlFragment frag)
        {
            return new SqlSegment(SqlSegmentType.Raw, frag);
        }

        return CreateParameter(context, value);
    }

    public virtual void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span)
    {
        var trimmed = span.Trim();

        if (trimmed.IsEmpty)
        {
            UpdateScannerState(context, span);

            return;
        }

        // Alias Capture
        if (context.ParseState.LastAliasableTarget is ISqlProjection projection)
        {
            if (TryPeekAlias(context, span, out var alias))
            {
                if (projection.Reference is ISqlReference entRef)
                {
                    entRef.Alias = alias;
                }

                context.ParseState.LastAliasableTarget = null;
            }
            else if (IsCaptureTerminated(span))
            {
                context.ParseState.LastAliasableTarget = null;
            }
        }

        // Signal that the NEXT hole is an alias label if this literal ends with "AS"
        context.ParseState.ExpectsAliasOnly = EndsWithAsKeyword(span); // ← this line was missing

        UpdateScannerState(context, span);
    }

    protected virtual SqlSegment CreateParameter(SqlContext context, object? value)
    {
        int index = context.Options.ParameterIndexStart + context.ParseState.ParameterCount;
        string prefix = context.Options.ParameterPrefixOverride ?? context.Dialect.ParameterPrefix;
        string paramKey = $"{prefix}{index}";
        
        context.Parameters[paramKey] = value ?? DBNull.Value;
        context.ParseState.ParameterCount++;
        
        return new SqlSegment(SqlSegmentType.Parameter, paramKey);
    }

    protected virtual bool TryPeekAlias(SqlContext context, ReadOnlySpan<char> span, out string? alias)
    {
        alias = null;
        var current = span;

        // 1. Skip leading whitespace
        int offset = 0;
        while (offset < current.Length && char.IsWhiteSpace(current[offset])) offset++;
        current = current[offset..];

        if (current.IsEmpty) return false;

        // 2. Handle optional "AS"
        if (current.StartsWith("AS", StringComparison.OrdinalIgnoreCase))
        {
            if (current.Length == 2 || !char.IsLetterOrDigit(current[2]))
            {
                current = current[2..];
                int ws = 0;
                while (ws < current.Length && char.IsWhiteSpace(current[ws])) ws++;
                current = current[ws..];
            }
        }

        // 3. Detect Quoted Alias (preserving WYSIWYG)
        string open = context.Dialect.OpenQuote;
        string close = context.Dialect.CloseQuote;

        if (!string.IsNullOrEmpty(open) && current.StartsWith(open))
        {
            int closeIdx = current[open.Length..].IndexOf(close);
            if (closeIdx != -1)
            {
                // Extract CLEAN name for internal logic, but original literal remains in SQL
                alias = current.Slice(open.Length, closeIdx).ToString();
                return true;
            }
        }

        // 4. Detect Unquoted Alias
        int end = 0;
        while (end < current.Length && (char.IsLetterOrDigit(current[end]) || current[end] == '_'))
        {
            end++;
        }

        if (end > 0)
        {
            var token = current[..end].ToString();
            if (IsSqlKeyword(token)) return false;
            alias = token;
            return true;
        }

        return false;
    }

    private bool EndsWithAsKeyword(ReadOnlySpan<char> span)
    {
        var trimmed = span.TrimEnd();
        
        // 1. Must be at least "AS"
        if (trimmed.Length < 2) return false;

        // 2. Check for "AS" at the end (Case-Insensitive)
        if (!trimmed.EndsWith("AS", StringComparison.OrdinalIgnoreCase)) 
            return false;

        // 3. If it's just "AS", it's a match
        if (trimmed.Length == 2) return true;

        // 4. Check the character immediately before the 'A'
        // Index is (Length of "AS" + 1) from the end
        char prefix = trimmed[trimmed.Length - 3];

        // In SQL, AS can follow whitespace, a closing paren, or brackets
        return char.IsWhiteSpace(prefix) || prefix == ')' || prefix == ']' || prefix == '"' || prefix == '`';
    }

    private void UpdateScannerState(SqlContext context, ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            var slice = span[i..];

            // String Tracking
            if (span[i] == '\'' && (i == 0 || span[i - 1] != '\\'))
            {
                context.ParseState.IsInsideString = !context.ParseState.IsInsideString;
                continue;
            }
            if (context.ParseState.IsInsideString) continue;

            // Comment Tracking
            if (slice.StartsWith("--"))
            {
                int nl = slice.IndexOfAny('\r', '\n');
                i += (nl == -1) ? slice.Length : nl;
                continue;
            }

            // Keyword Tracking
            if (i == 0 || char.IsWhiteSpace(span[i - 1]))
            {
                foreach (var keyword in SqlKeyword.AllInitiatorsOrdered)
                {
                    if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        if (slice.Length == keyword.Value.Length || !char.IsLetterOrDigit(slice[keyword.Value.Length]))
                        {
                            context.ParseState.CurrentKeyword = keyword;
                            i += keyword.Value.Length - 1;
                            break;
                        }
                    }
                }
            }
        }
    }

    protected virtual bool IsCaptureTerminated(ReadOnlySpan<char> span)
    {
        int i = 0;
        while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
        if (i >= span.Length) return false;

        char c = span[i];
        return c == ',' || c == ')' || c == ';' || c == '(';
    }

    private bool IsSqlKeyword(string word) => 
        SqlKeyword.AllKeywords.Any(k => k.Value.Equals(word, StringComparison.OrdinalIgnoreCase));
}