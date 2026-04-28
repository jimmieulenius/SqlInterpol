using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

internal static class SqlParser
{
    public static void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span)
    {
        // 1. Sniff for Aliases if the context is "waiting" for one
        if (context.PendingAliasCapture != null)
        {
            if (TryCaptureAlias(span, out var alias, out int consumed))
            {
                context.PendingAliasCapture.Reference.Alias = alias;
                context.PendingAliasCapture = null;
                span = span.Slice(consumed);
            }
            else if (IsCaptureTerminated(span))
            {
                context.PendingAliasCapture = null;
            }
        }

        // 2. Scan for keywords to update the current SQL clause state
        UpdateClauseState(context, span);
    }

    private static void UpdateClauseState(SqlContext context, ReadOnlySpan<char> span)
    {
        // This logic helps the builder know if a table should render 
        // as a "Declaration" (Table Name) or a "Reference" (Alias)
        for (int i = 0; i < span.Length; i++)
        {
            // Only check for keywords at word boundaries
            if (i == 0 || char.IsWhiteSpace(span[i - 1]))
            {
                var slice = span.Slice(i);
                foreach (var keyword in SqlKeyword.AllInitiatorsOrdered)
                {
                    if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        // Ensure it's the whole word (e.g., "SELECT" not "SELECTION")
                        if (slice.Length == keyword.Value.Length || !char.IsLetterOrDigit(slice[keyword.Value.Length]))
                        {
                            // context.CurrentKeyword = keyword;
                            i += keyword.Value.Length; // Advance past the keyword
                            break; 
                        }
                    }
                }
            }
        }
    }

    public static bool IsCaptureTerminated(ReadOnlySpan<char> span)
    {
        var current = span;
        if (!SkipWhitespaceAndComments(ref current)) return false;

        // If the next character is a comma, closing paren, or semicolon, 
        // it's impossible for a table alias to follow.
        char c = current[0];
        return c == ',' || c == ')' || c == ';' || c == '(';
    }

    public static bool TryCaptureAlias(ReadOnlySpan<char> span, out string? alias, out int consumed)
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

    public static bool SkipWhitespaceAndComments(ref ReadOnlySpan<char> span)
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

    public static bool IsSqlKeyword(string word) => 
        SqlKeyword.AllKeywords.Any(k => k.Value.Equals(word, StringComparison.OrdinalIgnoreCase));

    // ... TryCaptureAlias, IsCaptureTerminated, and SkipWhitespaceAndComments ...
}