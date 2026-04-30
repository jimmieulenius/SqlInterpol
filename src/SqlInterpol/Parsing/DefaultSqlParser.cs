using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

public class DefaultSqlParser : ISqlParser
{
    public void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span)
    {
        // 1. Alias Sniffing
        if (context.State.PendingAliasCapture != null)
        {
            if (TryCaptureAlias(span, out var alias, out int consumed))
            {
                context.State.PendingAliasCapture.Reference.Alias = alias;
                context.State.PendingAliasCapture = null;
                span = span[consumed..];
            }
            else if (IsCaptureTerminated(span))
            {
                context.State.PendingAliasCapture = null;
            }
        }

        // 2. Lexical Scanning
        for (int i = 0; i < span.Length; i++)
        {
            var slice = span[i..];

            // Handle Strings
            if (span[i] == '\'' && (i == 0 || span[i - 1] != '\\'))
            {
                context.State.IsInsideString = !context.State.IsInsideString;
                continue;
            }

            if (context.State.IsInsideString) continue;

            // Handle Comments
            if (slice.StartsWith("--"))
            {
                int nl = slice.IndexOfAny('\r', '\n');
                i += (nl == -1) ? slice.Length : nl;
                continue;
            }
            if (slice.StartsWith("/*"))
            {
                int end = slice[2..].IndexOf("*/");
                i += (end == -1) ? slice.Length : end + 3;
                continue;
            }

            // Detect Keywords
            if (i == 0 || char.IsWhiteSpace(span[i - 1]))
            {
                foreach (var keyword in SqlKeyword.AllInitiatorsOrdered)
                {
                    if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        if (slice.Length == keyword.Value.Length || !char.IsLetterOrDigit(slice[keyword.Value.Length]))
                        {
                            context.State.CurrentKeyword = keyword;
                            i += keyword.Value.Length - 1; 
                            break;
                        }
                    }
                }
            }
        }
    }

    public virtual SqlSegment ProcessValue(SqlContext context, object? value)
    {
        switch (value)
        {
            case ISqlProjection projection:
                // Parser manages the alias sniffing state
                context.State.PendingAliasCapture = projection; 
                return new SqlSegment(SqlSegmentType.Projection, projection, context.State.CurrentKeyword);

            case ISqlReference reference:
                return new SqlSegment(SqlSegmentType.Reference, reference);

            case ISqlFragment fragment:
                return new SqlSegment(SqlSegmentType.Fragment, fragment);

            // case string s:
            //     return CreateParameter(context, s);

            default:
                return CreateParameter(context, value);
        }
    }

    protected virtual SqlSegment CreateParameter(SqlContext context, object? value)
    {
        // Calculate index based on the starting offset
        int index = context.Options.ParameterIndexStart + context.State.ParameterCount;
        
        // Use the override if present, otherwise the Dialect's prefix
        string prefix = context.Options.ParameterPrefixOverride ?? context.Dialect.ParameterPrefix;
        
        string paramKey = $"{prefix}{index}";
        
        context.Parameters[paramKey] = value ?? DBNull.Value;
        context.State.ParameterCount++;
        
        return new SqlSegment(SqlSegmentType.Parameter, paramKey);
    }

    protected virtual bool TryCaptureAlias(ReadOnlySpan<char> span, out string? alias, out int consumed)
    {
        alias = null;
        consumed = 0;
        var current = span;

        if (!SkipWhitespaceAndComments(ref current)) return false;

        if (current.StartsWith("AS", StringComparison.OrdinalIgnoreCase))
        {
            if (current.Length == 2 || !char.IsLetterOrDigit(current[2]))
            {
                current = current[2..];
                if (!SkipWhitespaceAndComments(ref current)) return false;
            }
        }

        int end = 0;

        while (end < current.Length && (char.IsLetterOrDigit(current[end]) || current[end] == '_'))
        {
            end++;
        }

        if (end > 0)
        {
            var token = current[..end].ToString();

            if (IsSqlKeyword(token))
            {
                return false;
            }

            alias = token;
            consumed = span.Length - current[end..].Length;

            return true;
        }

        return false;
    }

    protected virtual bool IsCaptureTerminated(ReadOnlySpan<char> span)
    {
        var current = span;

        if (!SkipWhitespaceAndComments(ref current))
        {
            return false;
        }

        char c = current[0];

        return c == ',' || c == ')' || c == ';' || c == '(';
    }

    private bool SkipWhitespaceAndComments(ref ReadOnlySpan<char> span)
    {
        while (span.Length > 0)
        {
            if (char.IsWhiteSpace(span[0]))
            {
                span = span[1..];
                continue;
            }

            if (span.StartsWith("--"))
            {
                int nl = span.IndexOfAny('\r', '\n');
                span = nl == -1 ? ReadOnlySpan<char>.Empty : span[nl..];
                continue;
            }

            break;
        }

        return span.Length > 0;
    }

    private bool IsSqlKeyword(string word) => 
        SqlKeyword.AllKeywords.Any(k => k.Value.Equals(word, StringComparison.OrdinalIgnoreCase));
}