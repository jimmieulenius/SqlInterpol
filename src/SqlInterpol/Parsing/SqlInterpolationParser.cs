using System.Collections;

namespace SqlInterpol.Parsing;

public class SqlInterpolationParser : ISqlInterpolationParser
{
    public static readonly SqlInterpolationParser Instance = new();

    public virtual SqlSegment ProcessValue(ISqlParserContext context, object? value)
    {
        bool isAlias = context.ParserState.ExpectsAliasOnly;
        context.ParserState.ExpectsAliasOnly = false; // consume immediately

        // NEW: Check if the user is passing a dynamic string alias via a hole: AS {{"stats"}}
        if (isAlias && value is string dynamicAlias)
        {
            if (context.ParserState.LastAliasableTarget is ISqlEntityBase targetEntity)
            {
                if (targetEntity.Reference is ISqlReference entRef) entRef.Alias = dynamicAlias;
            }
            else if (context.ParserState.LastAliasableTarget is ISqlProjection targetProj)
            {
                if (targetProj.Reference is ISqlReference entRef) entRef.Alias = dynamicAlias;
            }

            context.ParserState.LastAliasableTarget = null;
            
            // Return a Raw segment with the quoted identifier (e.g. [stats])
            // We DO NOT want aliases to become parameters (@p0)!
            return new SqlSegment(SqlSegmentType.Raw, context.Dialect.QuoteIdentifier(dynamicAlias));
        }

        // Clean up if it was expected to be an alias but wasn't a valid string
        if (isAlias)
        {
            context.ParserState.LastAliasableTarget = null;
        }

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
            if (frag is ISqlParameterGenerator generator)
            {
                generator.GenerateParameters(context);
            }

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

            return new SqlSegment(SqlSegmentType.Raw, new SqlRawCollectionFragment(paramKeys));
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

        bool endsWithAs = EndsWithAsKeyword(span);

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
            else if (endsWithAs)
            {
                // DO NOT clear LastAliasableTarget! 
                // The literal ends with AS, so the alias name is coming in the next interpolation hole.
            }
            else if (IsCaptureTerminated(span))
            {
                context.ParserState.LastAliasableTarget = null;
            }
        }

        // Signal that the NEXT hole is an alias label if this literal ends with "AS"
        context.ParserState.ExpectsAliasOnly = endsWithAs;

        UpdateScannerState(context, span);
    }

    protected virtual SqlSegment CreateParameter(ISqlParserContext context, object? value)
    {
        string paramKey = context.AddParameter(value);

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
}