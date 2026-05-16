using System.Buffers;
using System.Collections;

namespace SqlInterpol.Parsing;

public class SqlInterpolationParser : ISqlInterpolationParser
{
    public static readonly SqlInterpolationParser Instance = new();

    public virtual SqlSegment ProcessValue(ISqlParserContext context, object? value)
    {
        bool isAlias = context.ParserState.ExpectsAliasOnly;
        context.ParserState.ExpectsAliasOnly = false; // consume immediately

        if (!isAlias)
        {
            context.ParserState.LastAliasableTarget = null;
        }

        // 1. Alias Handling
        if (isAlias && (value is string || (value is ISqlFragment && value is not ISqlProjection && value is not ISqlEntityBase)))
        {
            string? rawAlias = null;
            string? renderedOutput = null;

            if (value is string stringAlias)
            {
                rawAlias = stringAlias;
                renderedOutput = context.Dialect.QuoteIdentifier(stringAlias);
            }
            else if (value is ISqlFragment fragmentAlias)
            {
                renderedOutput = fragmentAlias.ToSql(context);
                rawAlias = context.Dialect.UnquoteIdentifier(renderedOutput);
            }

            if (rawAlias != null && renderedOutput != null)
            {
                if (context.ParserState.LastAliasableTarget is ISqlEntityBase targetEntity)
                {
                    if (targetEntity.Reference is ISqlReference entRef) entRef.Alias = rawAlias;
                }
                else if (context.ParserState.LastAliasableTarget is ISqlProjection targetProj)
                {
                    if (targetProj.Reference is ISqlReference entRef) entRef.Alias = rawAlias;
                }

                context.ParserState.LastAliasableTarget = null;
                return new SqlSegment(SqlSegmentType.Raw, renderedOutput);
            }
        }

        // 2. Projections
        if (value is ISqlProjection projection)
        {
            SqlRenderMode? mode = isAlias ? SqlRenderMode.AliasOnly : null;

            if (mode == null && context.ParserState.CurrentKeyword?.Value == SqlKeyword.Insert)
            {
                mode = SqlRenderMode.BaseName;
            }

            if (!isAlias) context.ParserState.LastAliasableTarget = projection;
            return new SqlSegment(SqlSegmentType.Projection, projection, mode);
        }

        // 3. Check for Tables/Subqueries (Entities)
        if (value is ISqlEntityBase entity)
        {
            if (context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.With, StringComparison.OrdinalIgnoreCase) == true)
            {
                context.ParserState.EntityRoles[entity] = SqlEntityRole.Cte;
                context.ParserState.CurrentKeyword = null;
                return new SqlSegment(SqlSegmentType.Reference, entity, SqlRenderMode.BaseName);
            }

            // FIX: If it's an Update, an Insert, OR if it's explicitly being aliased (AS sq), 
            // guarantee it becomes the Active Target for DTO mapping!
            if (context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.Update, StringComparison.OrdinalIgnoreCase) == true ||
                context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase) == true ||
                isAlias)
            {
                context.ParserState.ActiveEntityTarget = entity;
            }

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

            return new SqlSegment(SqlSegmentType.Reference, entity, isAlias ? SqlRenderMode.AliasOnly : null);
        }

        if (value is SqlOrderDirection direction)
        {
            return new SqlSegment(SqlSegmentType.Literal, direction == SqlOrderDirection.Asc ? SqlKeyword.Asc : SqlKeyword.Desc);
        }

        // 4. Magic DTO Promotion
        if (value != null && value.GetType().IsClass && value is not ISqlFragment && value is not ISqlReference && value is not ISqlEntityBase && value is not ISqlProjection && value is not string && value is not IEnumerable)
        {
            if (context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.Set, StringComparison.OrdinalIgnoreCase) == true && context.ParserState.ActiveEntityTarget != null)
            {
                var assignments = Sql.BuildAssignments(context.ParserState.ActiveEntityTarget, value);
                foreach (var assignment in assignments)
                {
                    if (assignment is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                }
                var fragment = new SqlSetFragment(assignments);
                return new SqlSegment(SqlSegmentType.Reference, fragment);
            }

            bool isInsert = context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase) == true;
            bool isValues = context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.Values, StringComparison.OrdinalIgnoreCase) == true;

            if ((isInsert || isValues) && context.ParserState.ActiveEntityTarget != null)
            {
                var assignments = Sql.BuildAssignments(context.ParserState.ActiveEntityTarget, value);
                foreach (var assignment in assignments)
                {
                    if (assignment is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                }
                var fragment = new SqlInsertValuesFragment(assignments);
                return new SqlSegment(SqlSegmentType.Reference, fragment);
            }
        }

        // 5. Bulk Inserts
        bool isInsertCmd = context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase) == true;
        bool isValuesCmd = context.ParserState.CurrentKeyword?.Value.Equals(SqlKeyword.Values, StringComparison.OrdinalIgnoreCase) == true;

        if ((isInsertCmd || isValuesCmd) && context.ParserState.ActiveEntityTarget != null && value is IEnumerable bulkEnumerable && value is not string && value is not byte[])
        {
            var bulkAssignments = new List<IEnumerable<ISqlAssignmentFragment>>();
            foreach (var item in bulkEnumerable)
            {
                if (item != null && item.GetType().IsClass && item is not string)
                {
                    var assignments = Sql.BuildAssignments(context.ParserState.ActiveEntityTarget, item);
                    foreach (var assignment in assignments)
                    {
                        if (assignment is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                    }
                    bulkAssignments.Add(assignments);
                }
            }

            if (bulkAssignments.Count > 0)
            {
                var fragment = new SqlInsertValuesFragment(bulkAssignments);
                return new SqlSegment(SqlSegmentType.Reference, fragment);
            }
        }

        // 6. Single Fragments
        if (value is ISqlFragment frag)
        {
            if (frag is ISqlParameterGenerator generator) generator.GenerateParameters(context);
            return new SqlSegment(SqlSegmentType.Raw, frag);
        }

        // 6.5 Collections of Fragments (Dynamic GROUP BY / ORDER BY lists)
        if (value is IEnumerable<ISqlFragment> fragmentCollection)
        {
            foreach (var item in fragmentCollection)
            {
                if (item is ISqlParameterGenerator gen) gen.GenerateParameters(context);
            }
            return new SqlSegment(SqlSegmentType.Raw, new SqlCollectionFragment(fragmentCollection));
        }

        // 7. Parameter Lists (IN clauses)
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

        // 8. Standard Parameters
        return CreateParameter(context, value);
    }

    public virtual string? ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span)
    {
        // 1. --- ZERO-ALLOCATION, STATE-AWARE SCRUBBING ---
        int length = span.Length;
        char[]? rented = null;
        Span<char> cleanBuffer = length <= 1024 
            ? stackalloc char[length] 
            : (rented = ArrayPool<char>.Shared.Rent(length));
        
        int cleanIndex = 0;

        for (int i = 0; i < length; i++)
        {
            char c = span[i];

            // 1. Toggle String State
            if (c == '\'' && !context.ParserState.InBlockComment && !context.ParserState.InLineComment)
            {
                // Handle escaped quotes ('') inside strings
                if (context.ParserState.IsInsideString && i + 1 < length && span[i + 1] == '\'')
                {
                    i++; // Skip the second quote entirely
                    continue; // Stay inside string state
                }

                context.ParserState.IsInsideString = !context.ParserState.IsInsideString;
                cleanBuffer[cleanIndex++] = ' '; // Pad with space to prevent word concatenation
                continue;
            }

            if (context.ParserState.IsInsideString)
            {
                continue; // Ignore string contents entirely
            }

            // 2. Block Comments
            if (!context.ParserState.InLineComment && c == '/' && i + 1 < length && span[i + 1] == '*')
            {
                context.ParserState.InBlockComment = true;
                i++; continue;
            }
            
            if (context.ParserState.InBlockComment && c == '*' && i + 1 < length && span[i + 1] == '/')
            {
                context.ParserState.InBlockComment = false;
                cleanBuffer[cleanIndex++] = ' '; // Pad with space
                i++; continue;
            }

            if (context.ParserState.InBlockComment)
            {
                continue; // Ignore multi-line comment contents
            }
            
            // 3. Line Comments
            if (!context.ParserState.InBlockComment && c == '-' && i + 1 < length && span[i + 1] == '-')
            {
                context.ParserState.InLineComment = true;
                i++; continue;
            }
            
            if (context.ParserState.InLineComment && (c == '\n' || c == '\r'))
            {
                context.ParserState.InLineComment = false;
                cleanBuffer[cleanIndex++] = c; // Keep newlines to separate lines safely
                continue;
            }

            if (context.ParserState.InLineComment)
            {
                continue; // Ignore single-line comment contents
            }

            // 4. Standard safe character (Not in a string or comment)
            cleanBuffer[cleanIndex++] = c;
        }

        var activeSpan = cleanBuffer.Slice(0, cleanIndex);

        // 3. --- KEYWORD SCANNING ON SCRUBBED SPAN ---
        var trimmed = activeSpan.Trim();
        string? tag = null;
        SqlKeyword? forcedKeyword = null; 

        if (trimmed.Contains(SqlKeyword.ForUpdate, StringComparison.OrdinalIgnoreCase))
            tag = SqlSegmentTag.ForUpdateKeyword;
        else if (trimmed.Contains(SqlKeyword.ForShare, StringComparison.OrdinalIgnoreCase))
            tag = SqlSegmentTag.ForShareKeyword;
        
        if (trimmed.Contains(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase))
            tag = SqlSegmentTag.Paging;
        else if (trimmed.EndsWith(SqlKeyword.DoUpdateSet, StringComparison.OrdinalIgnoreCase))
        {
            forcedKeyword = SqlKeyword.Set;
            tag = SqlSegmentTag.DoUpdateSetKeyword;
        }
        else if (trimmed.EndsWith(SqlKeyword.OnConflict, StringComparison.OrdinalIgnoreCase))
            tag = SqlSegmentTag.OnConflictKeyword;
        else if (trimmed.EndsWith(SqlKeyword.Update, StringComparison.OrdinalIgnoreCase))
            forcedKeyword = SqlKeyword.Update;
        else if (trimmed.EndsWith(SqlKeyword.Set, StringComparison.OrdinalIgnoreCase))
        {
            forcedKeyword = SqlKeyword.Set;
            tag = SqlSegmentTag.UpdateSetKeyword;
        }
        else if (trimmed.EndsWith($"{SqlKeyword.Insert} {SqlKeyword.Into}", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith(SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase))
            forcedKeyword = SqlKeyword.Insert;
        else if (trimmed.EndsWith(SqlKeyword.Values, StringComparison.OrdinalIgnoreCase))
        {
            forcedKeyword = SqlKeyword.Values;
            tag = SqlSegmentTag.InsertValuesKeyword;
        }
        else if (trimmed.EndsWith(SqlKeyword.Returning, StringComparison.OrdinalIgnoreCase))
            tag = SqlSegmentTag.ReturningKeyword;

        if (trimmed.IsEmpty)
        {
            UpdateScannerState(context, activeSpan);
            if (forcedKeyword != null) context.ParserState.CurrentKeyword = forcedKeyword;
            if (rented != null) ArrayPool<char>.Shared.Return(rented);
            return tag;
        }

        bool endsWithAs = EndsWithAsKeyword(activeSpan);

        if (context.ParserState.LastAliasableTarget != null)
        {
            if (TryPeekAlias(context, activeSpan, out var alias))
            {
                if (context.ParserState.LastAliasableTarget is ISqlEntityBase entity && entity.Reference is ISqlReference entRef)
                    entRef.Alias = alias;
                else if (context.ParserState.LastAliasableTarget is ISqlProjection projection && projection.Reference is ISqlReference projRef)
                    projRef.Alias = alias;
                
                context.ParserState.LastAliasableTarget = null;
            }
            else if (!endsWithAs)
            {
                context.ParserState.LastAliasableTarget = null;
            }
        }

        context.ParserState.ExpectsAliasOnly = endsWithAs;
        
        // Notice we pass `activeSpan` down to `UpdateScannerState` as well,
        // so it won't trigger `SqlKeyword` states (like `FROM`) if they are inside strings!
        UpdateScannerState(context, activeSpan);

        if (forcedKeyword != null)
            context.ParserState.CurrentKeyword = forcedKeyword;

        if (rented != null) ArrayPool<char>.Shared.Return(rented);

        return tag;
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

        if (current[0] == ')')
        {
            current = current[1..];
            int closeParen = 0;
            while (closeParen < current.Length && char.IsWhiteSpace(current[closeParen])) closeParen++;
            current = current[closeParen..];
        }

        if (current.IsEmpty) return false;

        if (current.StartsWith(SqlKeyword.As, StringComparison.OrdinalIgnoreCase))
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

            if (IsSqlKeyword(token) || 
                token.Equals(SqlKeyword.Returning, StringComparison.OrdinalIgnoreCase) ||
                token.Equals(SqlKeyword.On, StringComparison.OrdinalIgnoreCase) ||
                token.Equals(SqlKeyword.Do, StringComparison.OrdinalIgnoreCase) ||
                token.Equals(SqlKeyword.For, StringComparison.OrdinalIgnoreCase)) 
                return false;
            
            alias = token;
            return true;
        }

        return false;
    }

    private bool EndsWithAsKeyword(ReadOnlySpan<char> span)
    {
        var trimmed = span.TrimEnd();
        if (trimmed.Length < 2) return false;
        if (!trimmed.EndsWith(SqlKeyword.As, StringComparison.OrdinalIgnoreCase)) return false;
        if (trimmed.Length == 2) return true;

        char prefix = trimmed[^3];
        return char.IsWhiteSpace(prefix) || prefix == ')' || prefix == ']' || prefix == '"' || prefix == '`';
    }

    private void UpdateScannerState(ISqlParserContext context, ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            var slice = span[i..];

            if (span[i] == '(') context.ParserState.ParenDepth++;
            else if (span[i] == ')') context.ParserState.ParenDepth--;

            if (context.ParserState.ParenDepth > 0) continue;

            if (i == 0 || char.IsWhiteSpace(span[i - 1]) || span[i - 1] == '(' || span[i - 1] == ')')
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