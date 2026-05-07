using System.Collections;

namespace SqlInterpol.Parsing;

public class SqlInterpolationParser : ISqlInterpolationParser
{
    public static readonly SqlInterpolationParser Instance = new();

    public virtual SqlSegment ProcessValue(ISqlParserContext context, object? value)
    {
        bool isAlias = context.ParserState.ExpectsAliasOnly;
        context.ParserState.ExpectsAliasOnly = false; // consume immediately

        // 1. Check for Columns/Projections
        if (value is ISqlProjection projection)
        {
            if (!isAlias)
            {
                context.ParserState.LastAliasableTarget = projection;
            }
            else
            {
                if (projection.Reference is ISqlReference entRef && entRef.Alias == null)
                {
                    entRef.Alias = entRef.FallbackAlias;
                }
            }

            return new SqlSegment(SqlSegmentType.Projection, projection, isAliasTarget: isAlias);
        }

        // 2. NEW: Check for Tables/Subqueries (Entities)
        if (value is ISqlEntityBase entity)
        {
            if (!isAlias)
            {
                context.ParserState.LastAliasableTarget = entity;
            }
            else
            {
                if (entity.Reference is ISqlReference entRef && entRef.Alias == null)
                {
                    entRef.Alias = entRef.FallbackAlias;
                }
            }

            return new SqlSegment(SqlSegmentType.Reference, entity, isAliasTarget: isAlias);
        }

        if (value is ISqlFragment frag)
        {
            return new SqlSegment(SqlSegmentType.Raw, frag);
        }

        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            var paramKeys = new List<string>();
            
            foreach (var item in enumerable)
            {
                var paramSegment = CreateParameter(context, item);
                paramKeys.Add((string)paramSegment.Value!);
            }

            return new SqlSegment(SqlSegmentType.Raw, new SqlCollectionFragment(paramKeys));
        }

        return CreateParameter(context, value);
    }

    public virtual void ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span)
    {
        var trimmed = span.Trim();

        if (trimmed.IsEmpty)
        {
            UpdateScannerState(context, span);
            return;
        }

        // 3. NEW: Support retroactive look-ahead aliasing for BOTH Entities and Projections
        if (context.ParserState.LastAliasableTarget != null)
        {
            if (TryPeekAlias(context, span, out var alias))
            {
                if (context.ParserState.LastAliasableTarget is ISqlEntityBase entity)
                {
                    if (entity.Reference is ISqlReference entRef) entRef.Alias = alias;
                }
                else if (context.ParserState.LastAliasableTarget is ISqlProjection projection)
                {
                    if (projection.Reference is ISqlReference entRef) entRef.Alias = alias;
                }

                context.ParserState.LastAliasableTarget = null;
            }
            else if (IsCaptureTerminated(span))
            {
                context.ParserState.LastAliasableTarget = null;
            }
        }

        // Signal that the NEXT hole is an alias label if this literal ends with "AS"
        context.ParserState.ExpectsAliasOnly = EndsWithAsKeyword(span);

        UpdateScannerState(context, span);
    }

    protected virtual SqlSegment CreateParameter(ISqlParserContext context, object? value)
    {
        int index = context.Options.ParameterIndexStart + context.ParserState.ParameterCount;
        string prefix = context.Options.ParameterPrefixOverride ?? context.Dialect.ParameterPrefix;
        string paramKey = $"{prefix}{index}";
        
        context.Parameters[paramKey] = value ?? DBNull.Value;
        context.ParserState.ParameterCount++;
        
        return new SqlSegment(SqlSegmentType.Parameter, paramKey);
    }

    protected virtual bool TryPeekAlias(ISqlParserContext context, ReadOnlySpan<char> span, out string? alias)
    {
        alias = null;
        var current = span;

        int offset = 0;
        while (offset < current.Length && char.IsWhiteSpace(current[offset])) offset++;
        current = current[offset..];

        if (current.IsEmpty) return false;

        // Skip over a closing paren before looking for AS alias
        // This supports the pattern: FROM ({{subquery}}) AS customAlias
        if (current[0] == ')')
        {
            current = current[1..];
            int closeParen = 0;
            while (closeParen < current.Length && char.IsWhiteSpace(current[closeParen])) closeParen++;
            current = current[closeParen..];
        }

        if (current.IsEmpty) return false;

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

        string open = context.Dialect.OpenQuote;
        string close = context.Dialect.CloseQuote;

        if (!string.IsNullOrEmpty(open) && current.StartsWith(open))
        {
            int closeIdx = current[open.Length..].IndexOf(close);
            if (closeIdx != -1)
            {
                alias = current.Slice(open.Length, closeIdx).ToString();
                return true;
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
            if (IsSqlKeyword(token)) return false;
            alias = token;
            return true;
        }

        return false;
    }

    private bool EndsWithAsKeyword(ReadOnlySpan<char> span)
    {
        var trimmed = span.TrimEnd();
        
        if (trimmed.Length < 2) return false;
        if (!trimmed.EndsWith("AS", StringComparison.OrdinalIgnoreCase)) return false;
        if (trimmed.Length == 2) return true;

        char prefix = trimmed[trimmed.Length - 3];
        return char.IsWhiteSpace(prefix) || prefix == ')' || prefix == ']' || prefix == '"' || prefix == '`';
    }

    private void UpdateScannerState(ISqlParserContext context, ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            var slice = span[i..];

            if (span[i] == '\'' && (i == 0 || span[i - 1] != '\\'))
            {
                context.ParserState.IsInsideString = !context.ParserState.IsInsideString;
                continue;
            }
            if (context.ParserState.IsInsideString) continue;

            if (slice.StartsWith("--"))
            {
                int nl = slice.IndexOfAny('\r', '\n');
                i += (nl == -1) ? slice.Length : nl;
                continue;
            }

            if (i == 0 || char.IsWhiteSpace(span[i - 1]))
            {
                foreach (var keyword in SqlKeyword.AllInitiatorsOrdered)
                {
                    if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        if (slice.Length == keyword.Value.Length || !char.IsLetterOrDigit(slice[keyword.Value.Length]))
                        {
                            context.ParserState.CurrentKeyword = keyword;
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
        SqlKeyword.AllKeywords.Any(k =>
            k.Value.Equals(word, StringComparison.OrdinalIgnoreCase) ||
            k.Value.StartsWith(word + " ", StringComparison.OrdinalIgnoreCase));

    // private bool IsSqlKeyword(string word) => 
    //     SqlKeyword.AllKeywords.Any(k => k.Value.Equals(word, StringComparison.OrdinalIgnoreCase));
}




// using System.Collections;

// namespace SqlInterpol.Parsing;

// public class SqlInterpolationParser : ISqlInterpolationParser
// {
//     public static readonly SqlInterpolationParser Instance = new();

//     public virtual SqlSegment ProcessValue(ISqlParserContext context, object? value)
//     {
//         bool isAlias = context.ParserState.ExpectsAliasOnly;
//         context.ParserState.ExpectsAliasOnly = false; // consume immediately

//         if (value is ISqlProjection projection)
//         {
//             if (!isAlias)
//             {
//                 context.ParserState.LastAliasableTarget = projection;
//             }
//             else
//             {
//                 if (projection.Reference is ISqlReference entRef && entRef.Alias == null)
//                 {
//                     entRef.Alias = entRef.FallbackAlias;
//                 }
//             }

//             return new SqlSegment(SqlSegmentType.Projection, projection, isAliasTarget: isAlias);
//         }

//         if (value is ISqlFragment frag)
//         {
//             return new SqlSegment(SqlSegmentType.Raw, frag);
//         }

//         if (value is IEnumerable enumerable && value is not string && value is not byte[])
//         {
//             var paramKeys = new List<string>();
            
//             foreach (var item in enumerable)
//             {
//                 var paramSegment = CreateParameter(context, item);
//                 paramKeys.Add((string)paramSegment.Value!);
//             }

//             return new SqlSegment(SqlSegmentType.Raw, new SqlCollectionFragment(paramKeys));
//         }

//         return CreateParameter(context, value);
//     }

//     public virtual void ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span)
//     {
//         var trimmed = span.Trim();

//         if (trimmed.IsEmpty)
//         {
//             UpdateScannerState(context, span);

//             return;
//         }

//         if (context.ParserState.LastAliasableTarget is ISqlProjection projection)
//         {
//             if (TryPeekAlias(context, span, out var alias))
//             {
//                 if (projection.Reference is ISqlReference entRef)
//                 {
//                     entRef.Alias = alias;
//                 }

//                 context.ParserState.LastAliasableTarget = null;
//             }
//             else if (IsCaptureTerminated(span))
//             {
//                 context.ParserState.LastAliasableTarget = null;
//             }
//         }

//         // Signal that the NEXT hole is an alias label if this literal ends with "AS"
//         context.ParserState.ExpectsAliasOnly = EndsWithAsKeyword(span); // ← this line was missing

//         UpdateScannerState(context, span);
//     }

//     protected virtual SqlSegment CreateParameter(ISqlParserContext context, object? value)
//     {
//         int index = context.Options.ParameterIndexStart + context.ParserState.ParameterCount;
//         string prefix = context.Options.ParameterPrefixOverride ?? context.Dialect.ParameterPrefix;
//         string paramKey = $"{prefix}{index}";
        
//         context.Parameters[paramKey] = value ?? DBNull.Value;
//         context.ParserState.ParameterCount++;
        
//         return new SqlSegment(SqlSegmentType.Parameter, paramKey);
//     }

//     protected virtual bool TryPeekAlias(ISqlParserContext context, ReadOnlySpan<char> span, out string? alias)
//     {
//         alias = null;
//         var current = span;

//         // 1. Skip leading whitespace
//         int offset = 0;
//         while (offset < current.Length && char.IsWhiteSpace(current[offset])) offset++;
//         current = current[offset..];

//         if (current.IsEmpty) return false;

//         // 2. Handle optional "AS"
//         if (current.StartsWith("AS", StringComparison.OrdinalIgnoreCase))
//         {
//             if (current.Length == 2 || !char.IsLetterOrDigit(current[2]))
//             {
//                 current = current[2..];
//                 int ws = 0;
//                 while (ws < current.Length && char.IsWhiteSpace(current[ws])) ws++;
//                 current = current[ws..];
//             }
//         }

//         // 3. Detect Quoted Alias (preserving WYSIWYG)
//         string open = context.Dialect.OpenQuote;
//         string close = context.Dialect.CloseQuote;

//         if (!string.IsNullOrEmpty(open) && current.StartsWith(open))
//         {
//             int closeIdx = current[open.Length..].IndexOf(close);
//             if (closeIdx != -1)
//             {
//                 // Extract CLEAN name for internal logic, but original literal remains in SQL
//                 alias = current.Slice(open.Length, closeIdx).ToString();
//                 return true;
//             }
//         }

//         // 4. Detect Unquoted Alias
//         int end = 0;
//         while (end < current.Length && (char.IsLetterOrDigit(current[end]) || current[end] == '_'))
//         {
//             end++;
//         }

//         if (end > 0)
//         {
//             var token = current[..end].ToString();
//             if (IsSqlKeyword(token)) return false;
//             alias = token;
//             return true;
//         }

//         return false;
//     }

//     private bool EndsWithAsKeyword(ReadOnlySpan<char> span)
//     {
//         var trimmed = span.TrimEnd();
        
//         // 1. Must be at least "AS"
//         if (trimmed.Length < 2) return false;

//         // 2. Check for "AS" at the end (Case-Insensitive)
//         if (!trimmed.EndsWith("AS", StringComparison.OrdinalIgnoreCase)) 
//             return false;

//         // 3. If it's just "AS", it's a match
//         if (trimmed.Length == 2) return true;

//         // 4. Check the character immediately before the 'A'
//         // Index is (Length of "AS" + 1) from the end
//         char prefix = trimmed[trimmed.Length - 3];

//         // In SQL, AS can follow whitespace, a closing paren, or brackets
//         return char.IsWhiteSpace(prefix) || prefix == ')' || prefix == ']' || prefix == '"' || prefix == '`';
//     }

//     private void UpdateScannerState(ISqlParserContext context, ReadOnlySpan<char> span)
//     {
//         for (int i = 0; i < span.Length; i++)
//         {
//             var slice = span[i..];

//             // String Tracking
//             if (span[i] == '\'' && (i == 0 || span[i - 1] != '\\'))
//             {
//                 context.ParserState.IsInsideString = !context.ParserState.IsInsideString;
//                 continue;
//             }
//             if (context.ParserState.IsInsideString) continue;

//             // Comment Tracking
//             if (slice.StartsWith("--"))
//             {
//                 int nl = slice.IndexOfAny('\r', '\n');
//                 i += (nl == -1) ? slice.Length : nl;
//                 continue;
//             }

//             // Keyword Tracking
//             if (i == 0 || char.IsWhiteSpace(span[i - 1]))
//             {
//                 foreach (var keyword in SqlKeyword.AllInitiatorsOrdered)
//                 {
//                     if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
//                     {
//                         if (slice.Length == keyword.Value.Length || !char.IsLetterOrDigit(slice[keyword.Value.Length]))
//                         {
//                             context.ParserState.CurrentKeyword = keyword;
//                             i += keyword.Value.Length - 1;
//                             break;
//                         }
//                     }
//                 }
//             }
//         }
//     }

//     protected virtual bool IsCaptureTerminated(ReadOnlySpan<char> span)
//     {
//         int i = 0;
//         while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
//         if (i >= span.Length) return false;

//         char c = span[i];
//         return c == ',' || c == ')' || c == ';' || c == '(';
//     }

//     private bool IsSqlKeyword(string word) => 
//         SqlKeyword.AllKeywords.Any(k => k.Value.Equals(word, StringComparison.OrdinalIgnoreCase));
// }