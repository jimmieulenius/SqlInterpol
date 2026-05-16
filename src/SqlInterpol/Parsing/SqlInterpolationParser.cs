using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SqlInterpol.Metadata;
using SqlInterpol.References;

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

        // 2. MAGIC SELECT PROMOTION
        // Explicitly guard against ISqlProjection AND ISqlQuery so subqueries retain WYSIWYG indentation!
        if (value is ISqlEntityBase selectEntity && 
            value is not ISqlProjection && 
            value is not ISqlQuery &&
            string.Equals(context.ParserState.CurrentKeyword?.Value, SqlKeyword.Select, StringComparison.OrdinalIgnoreCase))
        {
            Type? modelType = null;
            Type type = selectEntity.GetType();

            if (type.IsGenericType)
            {
                modelType = type.GetGenericArguments()[0];
            }
            else
            {
                foreach (var i in type.GetInterfaces())
                {
                    if (i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ISqlEntityBase<>) || i.Name.StartsWith("ISqlEntity")))
                    {
                        modelType = i.GetGenericArguments()[0];
                        break;
                    }
                }
            }

            if (modelType != null)
            {
                var meta = SqlMetadataRegistry.GetMetadata(modelType);
                if (meta.Columns.Count > 0)
                {
                    var columns = new List<ISqlFragment>(meta.Columns.Count);
                    foreach (var kvp in meta.Columns)
                    {
                        columns.Add(new SqlColumnReference(selectEntity.Reference, kvp.Value, kvp.Key.Name));
                    }
                    return new SqlSegment(SqlSegmentType.Raw, new SqlCollectionFragment(columns));
                }
            }
        }

        // 3. Projections
        if (value is ISqlProjection projection)
        {
            SqlRenderMode? mode = isAlias ? SqlRenderMode.AliasOnly : null;

            if (mode == null && string.Equals(context.ParserState.CurrentKeyword?.Value, SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase))
            {
                mode = SqlRenderMode.BaseName;
            }

            if (!isAlias) context.ParserState.LastAliasableTarget = projection;
            return new SqlSegment(SqlSegmentType.Projection, projection, mode);
        }

        // 4. Check for Tables/CTEs (Entities in FROM/UPDATE/INSERT clauses)
        if (value is ISqlEntityBase entity)
        {
            string? currentKeyword = context.ParserState.CurrentKeyword?.Value;

            if (string.Equals(currentKeyword, SqlKeyword.With, StringComparison.OrdinalIgnoreCase))
            {
                context.ParserState.EntityRoles[entity] = SqlEntityRole.Cte;
                context.ParserState.CurrentKeyword = null;
                return new SqlSegment(SqlSegmentType.Reference, entity, SqlRenderMode.BaseName);
            }

            if (string.Equals(currentKeyword, SqlKeyword.Update, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentKeyword, SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase) ||
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

        // 5. Magic DTO Promotion
        if (value != null && value.GetType().IsClass && value is not ISqlFragment && value is not ISqlReference && value is not ISqlEntityBase && value is not ISqlProjection && value is not string && value is not IEnumerable)
        {
            string? currentKeyword = context.ParserState.CurrentKeyword?.Value;

            if (string.Equals(currentKeyword, SqlKeyword.Set, StringComparison.OrdinalIgnoreCase) && context.ParserState.ActiveEntityTarget != null)
            {
                var assignments = Sql.BuildAssignments(context.ParserState.ActiveEntityTarget, value);
                foreach (var assignment in assignments)
                {
                    if (assignment is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                }
                var fragment = new SqlSetFragment(assignments);
                return new SqlSegment(SqlSegmentType.Reference, fragment);
            }

            bool isInsert = string.Equals(currentKeyword, SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase);
            bool isValues = string.Equals(currentKeyword, SqlKeyword.Values, StringComparison.OrdinalIgnoreCase);

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

        // 6. Bulk Inserts
        string? bulkKeyword = context.ParserState.CurrentKeyword?.Value;
        bool isInsertCmd = string.Equals(bulkKeyword, SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase);
        bool isValuesCmd = string.Equals(bulkKeyword, SqlKeyword.Values, StringComparison.OrdinalIgnoreCase);

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

        // 7. Single Fragments
        if (value is ISqlFragment frag)
        {
            if (frag is ISqlParameterGenerator generator) generator.GenerateParameters(context);
            return new SqlSegment(SqlSegmentType.Raw, frag);
        }

        // 7.5 Collections of Fragments (Dynamic GROUP BY / ORDER BY lists)
        if (value is IEnumerable<ISqlFragment> fragmentCollection)
        {
            foreach (var item in fragmentCollection)
            {
                if (item is ISqlParameterGenerator gen) gen.GenerateParameters(context);
            }
            return new SqlSegment(SqlSegmentType.Raw, new SqlCollectionFragment(fragmentCollection));
        }

        // 8. Parameter Lists (IN clauses)
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

        // 9. Standard Parameters
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

            if (c == '\'' && !context.ParserState.InBlockComment && !context.ParserState.InLineComment)
            {
                if (context.ParserState.IsInsideString && i + 1 < length && span[i + 1] == '\'')
                {
                    i++; 
                    continue; 
                }

                context.ParserState.IsInsideString = !context.ParserState.IsInsideString;
                cleanBuffer[cleanIndex++] = ' '; 
                continue;
            }

            if (context.ParserState.IsInsideString) continue;

            if (!context.ParserState.InLineComment && c == '/' && i + 1 < length && span[i + 1] == '*')
            {
                context.ParserState.InBlockComment = true;
                i++; continue;
            }
            
            if (context.ParserState.InBlockComment && c == '*' && i + 1 < length && span[i + 1] == '/')
            {
                context.ParserState.InBlockComment = false;
                cleanBuffer[cleanIndex++] = ' '; 
                i++; continue;
            }

            if (context.ParserState.InBlockComment) continue; 
            
            if (!context.ParserState.InBlockComment && c == '-' && i + 1 < length && span[i + 1] == '-')
            {
                context.ParserState.InLineComment = true;
                i++; continue;
            }
            
            if (context.ParserState.InLineComment && (c == '\n' || c == '\r'))
            {
                context.ParserState.InLineComment = false;
                cleanBuffer[cleanIndex++] = c; 
                continue;
            }

            if (context.ParserState.InLineComment) continue;

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
        // THESE TWO LINES ARE CRITICAL to escape the SELECT state!
        else if (trimmed.EndsWith("SELECT", StringComparison.OrdinalIgnoreCase)) 
            forcedKeyword = SqlKeyword.Select;
        else if (trimmed.EndsWith("FROM", StringComparison.OrdinalIgnoreCase)) 
            forcedKeyword = SqlKeyword.From;

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
                bool matchedInitiator = false;
                foreach (var keyword in SqlKeyword.AllInitiatorsOrdered)
                {
                    if (slice.StartsWith(keyword.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        if (slice.Length == keyword.Value.Length || !char.IsLetterOrDigit(slice[keyword.Value.Length]))
                        {
                            context.ParserState.CurrentKeyword = keyword;
                            i += keyword.Value.Length - 1;
                            matchedInitiator = true;
                            break;
                        }
                    }
                }

                // FIX: If not an initiator, natively detect FROM to safely exit the SELECT state!
                // This ensures entities in the FROM clause are treated as tables, not columns.
                if (!matchedInitiator && slice.StartsWith("FROM", StringComparison.OrdinalIgnoreCase))
                {
                    if (slice.Length == 4 || !char.IsLetterOrDigit(slice[4]))
                    {
                        context.ParserState.CurrentKeyword = SqlKeyword.From;
                        i += 3;
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