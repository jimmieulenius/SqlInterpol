using System.Collections;
using System.Text;
using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

public class DefaultSqlParser : ISqlParser
{
    public SqlSegment ProcessValue(SqlContext context, object? value)
    {
        SqlSegment segment;

        if (value is ISqlProjection projection)
        {
            context.State.LastAliasableTarget = projection;
            
            // If we are currently in an 'AS' context (ExpectsAliasOnly), 
            // we must ensure the PREVIOUS table segment doesn't also render an 'AS'.
            if (context.State.ExpectsAliasOnly)
            {
                DowngradeLastSegmentIfDeclaration(context);
            }

            var mode = context.State.ExpectsAliasOnly ? SqlRenderMode.AliasOnly : SqlRenderMode.Default;
            if (mode == SqlRenderMode.Default && context.State.CurrentKeyword?.ExpectsDeclaration == true)
            {
                mode = SqlRenderMode.Declaration;
            }

            context.State.ExpectsAliasOnly = false;
            segment = new SqlSegment(SqlSegmentType.Projection, projection, context.State.CurrentKeyword, mode);
        }
        else
        {
            // If the hole is a manual alias (like p.Alias("prd")), trigger the downgrade
            if (context.State.ExpectsAliasOnly)
            {
                DowngradeLastSegmentIfDeclaration(context);
            }
            
            segment = value is ISqlFragment frag 
                ? new SqlSegment(SqlSegmentType.Raw, frag) 
                : CreateParameter(context, value);
        }

        context.State.LastSegment = segment;
        return segment;
    }

    private void DowngradeLastSegmentIfDeclaration(SqlContext context)
    {
        // We check the Builder's history to support split .Append() calls
        if (context.Builder.LastSegment is { RenderMode: SqlRenderMode.Declaration } last)
        {
            last.RenderMode = SqlRenderMode.BaseName;
        }
    }

    public void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span)
    {
        var trimmed = span.Trim();
        if (trimmed.IsEmpty)
        {
            UpdateScannerState(context, span);
            return;
        }

        // A. The "Lookback": Does this literal provide an alias for the PREVIOUS hole?
        // Example: "{p[Id]} AS ProductId" -> span starts with "AS"
        if (span.TrimStart().StartsWith("AS ", StringComparison.OrdinalIgnoreCase))
        {
            // If the previous segment was a table declaration, downgrade it to BaseName
            var last = GetLastSegment(context);

            if (last is { RenderMode: SqlRenderMode.Declaration })
            {
                last.RenderMode = SqlRenderMode.BaseName;
            }
            
            // Note: We do NOT set ExpectsAliasOnly = true here. 
            // That flag is only for when the hole ITSELF is the alias.
        }

        // B. The "Lookahead": Does this literal end with "AS", meaning the NEXT hole is the alias?
        // Example: "FROM {p} AS {p.Alias("prd")}" -> span ends with "AS"
        context.State.ExpectsAliasOnly = EndsWithAsKeyword(span);

        // C. The "Time Traveler" Capture (Existing)
        if (context.State.LastAliasableTarget is ISqlProjection projection)
        {
            if (TryPeekAlias(context, span, out var alias))
            {
                if (projection.Reference is ISqlReference entRef)
                {
                    entRef.Alias = alias;
                    // If we captured a manual alias from the string, ensure no double AS
                    var last = GetLastSegment(context);

                    if (last is { RenderMode: SqlRenderMode.Declaration })
                    {
                        last.RenderMode = SqlRenderMode.BaseName;
                    }
                }
                context.State.LastAliasableTarget = null;
            }
            else if (IsCaptureTerminated(span))
            {
                context.State.LastAliasableTarget = null;
            }
        }

        UpdateScannerState(context, span);
    }

    private SqlSegment? GetLastSegment(SqlContext context)
    {
        // 1. Check the active state (current string)
        // 2. Fallback to the builder (previous strings)
        return context.State.LastSegment ?? context.Builder.LastSegment;
    }

    protected virtual SqlSegment HandleCollection(SqlContext context, IEnumerable collection)
    {
        var sb = new StringBuilder();
        bool first = true;

        foreach (var item in collection)
        {
            if (!first) sb.Append(", ");
            var paramSegment = CreateParameter(context, item);
            sb.Append(paramSegment.Value); // Append the param name (e.g. @p1)
            first = false;
        }

        if (first) sb.Append("NULL"); // Handle empty collections safely

        // We return as Raw because the string now contains the parameter names
        return new SqlSegment(SqlSegmentType.Raw, sb.ToString());
    }

    protected virtual SqlSegment CreateParameter(SqlContext context, object? value)
    {
        int index = context.Options.ParameterIndexStart + context.State.ParameterCount;
        string prefix = context.Options.ParameterPrefixOverride ?? context.Dialect.ParameterPrefix;
        string paramKey = $"{prefix}{index}";
        
        context.Parameters[paramKey] = value ?? DBNull.Value;
        context.State.ParameterCount++;
        
        return new SqlSegment(SqlSegmentType.Parameter, paramKey);
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

    private SqlSegment HandleProjection(SqlContext context, ISqlProjection projection)
    {
        // Mark that the next literal might contain an alias for this projection
        context.State.PendingAliasCapture = projection;
        return new SqlSegment(SqlSegmentType.Projection, projection, context.State.CurrentKeyword);
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

    private void UpdateScannerState(SqlContext context, ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            var slice = span[i..];

            // String Tracking
            if (span[i] == '\'' && (i == 0 || span[i - 1] != '\\'))
            {
                context.State.IsInsideString = !context.State.IsInsideString;
                continue;
            }
            if (context.State.IsInsideString) continue;

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
                            context.State.CurrentKeyword = keyword;
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