using System.Collections;
using System.Text;
using SqlInterpol.Config;
using SqlInterpol.References;

namespace SqlInterpol.Parsing;

public class DefaultSqlParser : ISqlParser
{
    public SqlSegment ProcessValue(SqlContext context, object? value)
    {
        // 1. Highest Priority: Projections (Entities/Columns)
        if (value is ISqlProjection projection)
        {
            // Set for "Time Traveler" Alias Capture
            context.State.LastAliasableTarget = projection;
            
            var mode = context.State.ExpectsAliasOnly ? SqlRenderMode.AliasOnly : SqlRenderMode.Default;
            context.State.ExpectsAliasOnly = false;

            return new SqlSegment(SqlSegmentType.Projection, projection, context.State.CurrentKeyword, mode);
        }

        // 2. Collections (WHERE IN support)
        // We exclude string because it is an IEnumerable but we want it as a parameter
        if (value is IEnumerable collection && value is not string)
        {
            return HandleCollection(context, collection);
        }

        // 3. Manual Fragments
        if (value is ISqlFragment fragment)
        {
            return new SqlSegment(SqlSegmentType.Raw, fragment);
        }

        // 4. Default: Standard Parameter
        return CreateParameter(context, value);
    }

    public void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span)
    {
        context.State.ExpectsAliasOnly = EndsWithAsKeyword(span);

        // The "Time Traveler" Capture logic
        if (context.State.LastAliasableTarget is ISqlProjection projection)
        {
            if (TryPeekAlias(context, span, out var alias))
            {
                // 1. We check if the reference is an EntityReference (the prefix owner)
                if (projection.Reference is EntityReference entRef)
                {
                    entRef.Alias = alias;
                }
                
                // 2. Clear the target once captured
                context.State.LastAliasableTarget = null;
            }
            else if (IsCaptureTerminated(span))
            {
                context.State.LastAliasableTarget = null;
            }
        }

        UpdateScannerState(context, span);
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
        if (trimmed.Length < 2) return false;
        
        // Match " AS" at the end of the string
        if (trimmed.EndsWith("AS", StringComparison.OrdinalIgnoreCase))
        {
            // Must be start of string or preceded by whitespace
            return trimmed.Length == 2 || char.IsWhiteSpace(trimmed[^(3)]);
        }
        return false;
    }

    private SqlSegment HandleProjection(SqlContext context, ISqlProjection projection)
    {
        // Mark that the next literal might contain an alias for this projection
        context.State.PendingAliasCapture = projection;
        return new SqlSegment(SqlSegmentType.Projection, projection, context.State.CurrentKeyword);
    }

    // protected virtual SqlSegment HandleCollection(SqlContext context, IEnumerable collection)
    // {
    //     var sb = new StringBuilder();
    //     bool first = true;

    //     foreach (var item in collection)
    //     {
    //         if (!first) sb.Append(", ");
            
    //         // Create a parameter for each item and get its full key (e.g., @p0)
    //         var paramSegment = CreateParameter(context, item);
    //         sb.Append(paramSegment.Value); 
            
    //         first = false;
    //     }

    //     if (first) sb.Append("NULL");

    //     // Return as Raw because the string already contains the final parameter names
    //     return new SqlSegment(SqlSegmentType.Raw, sb.ToString());
    // }

    // protected virtual SqlSegment CreateParameter(SqlContext context, object? value)
    // {
    //     int index = context.Options.ParameterIndexStart + context.State.ParameterCount;
    //     string prefix = context.Options.ParameterPrefixOverride ?? context.Dialect.ParameterPrefix;
        
    //     // This is the FINAL identifier (e.g. "@p0", "$1", "!!100")
    //     string paramKey = $"{prefix}{index}";
        
    //     context.Parameters[paramKey] = value ?? DBNull.Value;
    //     context.State.ParameterCount++;
        
    //     // The Renderer should append this Value exactly as is
    //     return new SqlSegment(SqlSegmentType.Parameter, paramKey);
    // }

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