using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

/// <summary>
/// The default implementation of <see cref="ISqlInterpolationParser"/> that drives keyword detection,
/// entity promotion, DTO mapping, parameter binding, and alias resolution during query construction.
/// </summary>
public class SqlInterpolationParser : ISqlInterpolationParser
{
    public static readonly SqlInterpolationParser Instance = new();

    private static readonly ConditionalWeakTable<ISqlParserState, Stack<SqlKeyword?>> _keywordStacks = new();

    private Stack<SqlKeyword?> GetKeywordStack(ISqlParserState state)
    {
        return _keywordStacks.GetValue(state, _ => new Stack<SqlKeyword?>());
    }

    /// <inheritdoc />
    public virtual SqlSegment ProcessValue(ISqlParserContext context, object? value)
    {
        bool isAlias = context.ParserState.ExpectsAliasOnly;
        context.ParserState.ExpectsAliasOnly = false; // consume immediately

        if (!isAlias)
        {
            context.ParserState.LastAliasableTarget = null;
        }

        // 1. Alias Handling
        if (isAlias)
        {
            if (value is ISqlEntityBase entityAlias)
            {
                if (entityAlias.Reference is ISqlReference reference)
                {
                    if (string.IsNullOrWhiteSpace(reference.Alias))
                    {
                        reference.Alias = reference.FallbackAlias;
                        reference.IsAliasQuoted = true; 
                    }
                }
                
                context.ParserState.LastAliasableTarget = null;
                
                string? currentKeywordAlias = context.ParserState.CurrentKeyword?.Value;
                if (string.Equals(currentKeywordAlias, SqlKeyword.Update, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(currentKeywordAlias, SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase))
                {
                    context.ParserState.ActiveEntityTarget = entityAlias;
                }
                
                return new SqlSegment(SqlSegmentType.Raw, entityAlias, SqlRenderMode.AliasOnly);
            }

            string? rawAlias = null;
            SqlSegment? segmentToReturn = null;
            bool isQuoted = true;

            if (value is string stringAlias)
            {
                rawAlias = stringAlias;
                segmentToReturn = new SqlSegment(SqlSegmentType.Literal, context.Dialect.QuoteIdentifier(stringAlias));
            }
            else if (value is ISqlProjection projAlias)
            {
                rawAlias = context.Dialect.UnquoteIdentifier(projAlias.ToSql(context, SqlRenderMode.AliasOnly));
                segmentToReturn = new SqlSegment(SqlSegmentType.Projection, projAlias, SqlRenderMode.AliasOnly);
            }
            else if (value is ISqlFragment fragmentAlias)
            {
                var renderedOutput = fragmentAlias.ToSql(context);
                rawAlias = context.Dialect.UnquoteIdentifier(renderedOutput);
                segmentToReturn = new SqlSegment(SqlSegmentType.Raw, fragmentAlias, SqlRenderMode.AliasOnly);
            }

            if (rawAlias != null && segmentToReturn != null)
            {
                if (context.ParserState.LastAliasableTarget is ISqlEntityBase targetEntity && targetEntity.Reference is ISqlReference entRef)
                {
                    entRef.Alias = rawAlias;
                    entRef.IsAliasQuoted = isQuoted;
                }
                else if (context.ParserState.LastAliasableTarget is ISqlProjection targetProj && targetProj.Reference is ISqlReference projRef)
                {
                    projRef.Alias = rawAlias;
                    projRef.IsAliasQuoted = isQuoted;
                }

                context.ParserState.LastAliasableTarget = null;
                return segmentToReturn;
            }
        }

        // 2. SELECT PROMOTION
        bool isSelect = context.ParserState.CurrentKeyword == SqlKeyword.Select;
        bool isSelectDistinct = context.ParserState.CurrentKeyword == SqlKeyword.SelectDistinct;

        if (value is ISqlEntityBase selectEntity && 
            value is not ISqlProjection && 
            value is not ISqlQuery &&
            (isSelect || isSelectDistinct))
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

                    // FIX: Sort by the mapped database column name (c.Value) rather than the C# property name!
                    foreach (var kvp in meta.Columns.OrderBy(c => c.Value))
                    {
                        columns.Add(new SqlColumnReference(selectEntity.Reference, kvp.Value, kvp.Key.Name));
                    }

                    return new SqlSegment(SqlSegmentType.Raw, new SqlSelectFragment(columns, isDistinct: isSelectDistinct));
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

        // 4.5 Template Macro Expansion
        if (value is ISqlExpandable expandable && context.ParserState.ActiveEntityTarget != null)
        {
            var activeTarget = context.ParserState.ActiveEntityTarget;
            Type targetType = activeTarget.GetType();
            
            Type? modelType = targetType.IsGenericType ? targetType.GetGenericArguments()[0] : null;
            if (modelType == null)
            {
                foreach (var i in targetType.GetInterfaces())
                {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISqlEntityBase<>))
                    {
                        modelType = i.GetGenericArguments()[0];
                        break;
                    }
                }
            }
            modelType ??= targetType;

            var dtoProps = SqlMetadataRegistry.GetDtoProperties(expandable.DtoType);
            var entityMeta = SqlMetadataRegistry.GetMetadata(modelType);
            
            // FIX: Sort DTO properties by their mapped SQL column name for perfectly deterministic structural layout
            var sortedDtoProps = dtoProps.OrderBy(p => 
            {
                var entityMember = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name == p.Name);
                return entityMember != null ? entityMeta.Columns[entityMember] : p.Name;
            }).ToArray();
            
            var assignments = new List<ISqlAssignmentFragment>();
            string? currentKeyword = context.ParserState.CurrentKeyword?.Value;
            bool isSetClause = string.Equals(currentKeyword, SqlKeyword.Set, StringComparison.OrdinalIgnoreCase);

            foreach (var prop in sortedDtoProps)
            {
                if (isSetClause && expandable.KeyProperties.Contains(prop.Name)) continue;

                var entityMember = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name == prop.Name);
                if (entityMember != null)
                {
                    var colRef = new SqlColumnReference(activeTarget.Reference, entityMeta.Columns[entityMember], prop.Name);
                    assignments.Add(new SqlAssignmentFragment(colRef, Sql.Arg(prop.Name))); 
                }
            }

            if (isSetClause)
            {
                return new SqlSegment(SqlSegmentType.Reference, new SqlSetFragment(assignments));
            }
            
            if (string.Equals(currentKeyword, SqlKeyword.Select, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(currentKeyword, SqlKeyword.SelectDistinct, StringComparison.OrdinalIgnoreCase))
            {
                var columns = assignments.Select(a => (ISqlFragment)a.Reference).ToList();
                return new SqlSegment(SqlSegmentType.Raw, new SqlSelectFragment(columns, isDistinct: string.Equals(currentKeyword, SqlKeyword.SelectDistinct, StringComparison.OrdinalIgnoreCase)));
            }
            
            return new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(assignments));
        }

        // 5. Magic DTO Promotion
        if (value != null && value.GetType().IsClass && value is not ISqlFragment && value is not ISqlReference && value is not ISqlEntityBase && value is not ISqlProjection && value is not string && value is not IEnumerable)
        {
            string? currentKeyword = context.ParserState.CurrentKeyword?.Value;

            if (string.Equals(currentKeyword, SqlKeyword.Set, StringComparison.OrdinalIgnoreCase) && context.ParserState.ActiveEntityTarget != null)
            {
                var assignments = Sql.BuildAssignments(context.ParserState.ActiveEntityTarget, value, context);
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
                var assignments = Sql.BuildAssignments(context.ParserState.ActiveEntityTarget, value, context);
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
            int itemCount = 0;
            if (bulkEnumerable is ICollection col) itemCount = col.Count;

            if (itemCount > 0)
            {
                Type? elementType = GetEnumerableElementType(bulkEnumerable.GetType());
                if (elementType != null)
                {
                    var dtoProps = SqlMetadataRegistry.GetDtoProperties(elementType);
                    int propertiesPerRow = dtoProps.Length;

                    if (propertiesPerRow > 0)
                    {
                        int expectedTotalParams = itemCount * propertiesPerRow;
                        int maxParams = context.Options.QueryParametersMaxCount ?? context.Dialect.QueryParametersMaxCount;

                        if (context.ParserState.ParameterCount + expectedTotalParams > maxParams)
                        {
                            throw new SqlParameterLimitException(maxParams, context.ParserState.ParameterCount + expectedTotalParams);
                        }
                    }
                }
            }

            var bulkAssignments = new List<IEnumerable<ISqlAssignmentFragment>>();
            foreach (var item in bulkEnumerable)
            {
                if (item != null && item.GetType().IsClass && item is not string)
                {
                    var assignments = Sql.BuildAssignments(context.ParserState.ActiveEntityTarget, item, context);
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
            int expectedCount = 0;
            if (enumerable is ICollection inCol) expectedCount = inCol.Count;

            if (expectedCount > 0)
            {
                int maxParams = context.Options.QueryParametersMaxCount ?? context.Dialect.QueryParametersMaxCount;
                if (context.ParserState.ParameterCount + expectedCount > maxParams)
                {
                    throw new SqlParameterLimitException(maxParams, context.ParserState.ParameterCount + expectedCount);
                }
            }

            var paramKeys = new List<string>(expectedCount > 0 ? expectedCount : 4);
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

    private enum KeywordMatchMode { EndsWithWord, ContainsWord, EqualsWord }

    private readonly record struct KeywordRule(string Text, KeywordMatchMode Mode, string? Tag, SqlKeyword? ForcedKeyword);

    private static readonly KeywordRule[] _keywordRules =
    [
        new(SqlKeyword.Create.Value,                   KeywordMatchMode.ContainsWord, SqlSegmentTag.CreateKeyword,        SqlKeyword.Create),
        new(SqlKeyword.Drop.Value,                     KeywordMatchMode.ContainsWord, SqlSegmentTag.DropKeyword,          SqlKeyword.Drop),
        new(SqlKeyword.Alter.Value,                    KeywordMatchMode.ContainsWord, SqlSegmentTag.AlterKeyword,         SqlKeyword.Alter),
        new(SqlKeyword.Truncate.Value,                 KeywordMatchMode.ContainsWord, SqlSegmentTag.TruncateKeyword,      SqlKeyword.Truncate),
        new(SqlKeyword.Delete.Value,                   KeywordMatchMode.ContainsWord, SqlSegmentTag.DeleteKeyword,        SqlKeyword.Delete),

        new(SqlKeyword.ForUpdate.Value,                KeywordMatchMode.ContainsWord, SqlSegmentTag.ForUpdateKeyword,     null),
        new(SqlKeyword.ForShare.Value,                 KeywordMatchMode.ContainsWord, SqlSegmentTag.ForShareKeyword,      null),
        new(SqlKeyword.Limit.Value,                    KeywordMatchMode.ContainsWord, SqlSegmentTag.Paging,               null),
        new(SqlKeyword.DoUpdateSet.Value,              KeywordMatchMode.EndsWithWord, SqlSegmentTag.DoUpdateSetKeyword,   SqlKeyword.Set),
        new(SqlKeyword.OnConflict.Value,               KeywordMatchMode.EndsWithWord, SqlSegmentTag.OnConflictKeyword,    null),
        new(SqlKeyword.Update.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.UpdateKeyword,        SqlKeyword.Update),
        new(SqlKeyword.Set.Value,                      KeywordMatchMode.EndsWithWord, SqlSegmentTag.SetKeyword,           SqlKeyword.Set),
        
        new($"{SqlKeyword.Insert.Value} {SqlKeyword.Into.Value}", KeywordMatchMode.ContainsWord, SqlSegmentTag.InsertKeyword, SqlKeyword.Insert), 
        new(SqlKeyword.Insert.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.InsertKeyword,        SqlKeyword.Insert),
        
        new(SqlKeyword.Values.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.InsertValuesKeyword,  SqlKeyword.Values),
        new(SqlKeyword.Into.Value,                     KeywordMatchMode.ContainsWord, SqlSegmentTag.IntoKeyword,          SqlKeyword.Into),
        new(SqlKeyword.Returning.Value,                KeywordMatchMode.EndsWithWord, SqlSegmentTag.ReturningKeyword,     null),
        new(SqlKeyword.SelectDistinct.Value,           KeywordMatchMode.EndsWithWord, SqlSegmentTag.SelectDistinctKeyword, SqlKeyword.SelectDistinct),
        new(SqlKeyword.Select.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.SelectKeyword,        SqlKeyword.Select),
        new(SqlKeyword.From.Value,                     KeywordMatchMode.EndsWithWord, SqlSegmentTag.FromKeyword,          SqlKeyword.From),
        new(SqlKeyword.Except.Value,                   KeywordMatchMode.EqualsWord,   SqlSegmentTag.ExceptKeyword,        null),
        new(SqlKeyword.Intersect.Value,                KeywordMatchMode.EqualsWord,   SqlSegmentTag.IntersectKeyword,     null),
        new(SqlKeyword.UnionAll.Value,                 KeywordMatchMode.EqualsWord,   SqlSegmentTag.UnionAllKeyword,      null),
        new(SqlKeyword.Union.Value,                    KeywordMatchMode.EqualsWord,   SqlSegmentTag.UnionKeyword,         null),
        new(SqlKeyword.Where.Value,                    KeywordMatchMode.ContainsWord, SqlSegmentTag.WhereKeyword,         SqlKeyword.Where),
    ];

    private static bool EndsWithWholeWord(ReadOnlySpan<char> text, string keyword)
    {
        if (!text.EndsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        int pos = text.Length - keyword.Length;
        return pos == 0 || !char.IsLetterOrDigit(text[pos - 1]);
    }

    private static bool ContainsWholeWord(ReadOnlySpan<char> text, string keyword)
    {
        var kw = keyword.AsSpan();
        int start = 0;
        while (start <= text.Length - kw.Length)
        {
            int idx = text[start..].IndexOf(kw, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            idx += start;
            bool leftOk  = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
            bool rightOk = idx + kw.Length >= text.Length || !char.IsLetterOrDigit(text[idx + kw.Length]);
            if (leftOk && rightOk) return true;
            start = idx + 1;
        }
        return false;
    }

    /// <inheritdoc />
    public virtual string? ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span)
    {
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
                    i++; continue; 
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
        var trimmed = activeSpan.Trim();
        string? tag = null;
        SqlKeyword? forcedKeyword = null;

        foreach (var rule in _keywordRules)
        {
            bool matched = rule.Mode switch
            {
                KeywordMatchMode.EndsWithWord => EndsWithWholeWord(trimmed, rule.Text),
                KeywordMatchMode.ContainsWord => ContainsWholeWord(trimmed, rule.Text),
                _                             => trimmed.Equals(rule.Text, StringComparison.OrdinalIgnoreCase),
            };

            if (!matched) continue;

            tag = rule.Tag;
            forcedKeyword = rule.ForcedKeyword;

            if (forcedKeyword == SqlKeyword.Into && context.ParserState.CurrentKeyword == SqlKeyword.Select)
            {
                tag = SqlSegmentTag.SelectIntoKeyword;
            }
            break;
        }

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
            if (TryPeekAlias(context, activeSpan, out var alias, out var isQuoted))
            {
                if (context.ParserState.LastAliasableTarget is ISqlEntityBase entity && entity.Reference is ISqlReference entRef)
                {
                    entRef.Alias = alias;
                    entRef.IsAliasQuoted = isQuoted;
                }
                else if (context.ParserState.LastAliasableTarget is ISqlProjection projection && projection.Reference is ISqlReference projRef)
                {
                    projRef.Alias = alias;
                    projRef.IsAliasQuoted = isQuoted;
                }
                
                context.ParserState.LastAliasableTarget = null;
            }
            else if (!endsWithAs)
            {
                context.ParserState.LastAliasableTarget = null;
            }
        }

        context.ParserState.ExpectsAliasOnly = endsWithAs;
        UpdateScannerState(context, activeSpan);

        if (forcedKeyword != null && context.ParserState.ParenDepth == 0)
        {
            context.ParserState.CurrentKeyword = forcedKeyword;
        }

        if (rented != null) ArrayPool<char>.Shared.Return(rented);
        return tag;
    }

    /// <inheritdoc />
    public virtual string ReplaceKeyword(string sql, string keyword, string replacement)
    {
        if (string.IsNullOrEmpty(sql) || sql.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) == -1)
            return sql;

        var result = new System.Text.StringBuilder(sql.Length + 10);
        bool inString = false, inLineComment = false, inBlockComment = false;
        int keywordLen = keyword.Length;

        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];

            if (c == '\'' && !inBlockComment && !inLineComment)
            {
                if (inString && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    result.Append("''");
                    i++; continue;
                }
                inString = !inString;
                result.Append(c);
                continue;
            }
            if (inString) { result.Append(c); continue; }

            if (!inLineComment && c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                inBlockComment = true; result.Append("/*"); i++; continue;
            }
            if (inBlockComment && c == '*' && i + 1 < sql.Length && sql[i + 1] == '/')
            {
                inBlockComment = false; result.Append("*/"); i++; continue;
            }
            if (inBlockComment) { result.Append(c); continue; }

            if (!inBlockComment && c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                inLineComment = true; result.Append("--"); i++; continue;
            }
            if (inLineComment && (c == '\n' || c == '\r'))
            {
                inLineComment = false; result.Append(c); continue;
            }
            if (inLineComment) { result.Append(c); continue; }

            if (i + keywordLen <= sql.Length && string.Compare(sql, i, keyword, 0, keywordLen, StringComparison.OrdinalIgnoreCase) == 0)
            {
                bool leftSafe = i == 0 || (!char.IsLetterOrDigit(sql[i - 1]) && sql[i - 1] != '_');
                bool rightSafe = i + keywordLen == sql.Length || (!char.IsLetterOrDigit(sql[i + keywordLen]) && sql[i + keywordLen] != '_');

                if (leftSafe && rightSafe)
                {
                    result.Append(replacement);
                    i += keywordLen - 1;
                    continue;
                }
            }

            result.Append(c);
        }

        return result.ToString();
    }

    protected virtual SqlSegment CreateParameter(ISqlParserContext context, object? value)
    {
        string paramKey = context.AddParameter(value);
        return new SqlSegment(SqlSegmentType.Parameter, paramKey);
    }

    protected virtual bool TryPeekAlias(ISqlParserContext context, ReadOnlySpan<char> span, out string? alias, out bool isQuoted)
    {
        alias = null;
        isQuoted = false;
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
                isQuoted = true; 
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
            isQuoted = false; 
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
        var trimmedSpan = span.Trim();

        if (trimmedSpan.EndsWith($"{SqlKeyword.Select} {SqlKeyword.Distinct}", StringComparison.OrdinalIgnoreCase))
        {
            context.ParserState.CurrentKeyword = SqlKeyword.SelectDistinct;
            return;
        }

        var stack = GetKeywordStack(context.ParserState);

        for (int i = 0; i < span.Length; i++)
        {
            var slice = span[i..];

            if (span[i] == '(')
            {
                context.ParserState.ParenDepth++;
                stack.Push(context.ParserState.CurrentKeyword);
            }
            else if (span[i] == ')')
            {
                context.ParserState.ParenDepth--;
                if (stack.Count > 0) context.ParserState.CurrentKeyword = stack.Pop();
            }

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

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];
        
        var enumInterface = type.GetInterfaces().FirstOrDefault(i => 
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            
        return enumInterface?.GetGenericArguments()[0];
    }
}