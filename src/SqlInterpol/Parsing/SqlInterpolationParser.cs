using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

public class SqlInterpolationParser : ISqlInterpolationParser
{
    public static readonly SqlInterpolationParser Instance = new();

    // WYSIWYG FIX: Invisibly attach a keyword stack to the parser state without breaking the interface!
    private static readonly ConditionalWeakTable<ISqlParserState, Stack<SqlKeyword?>> _keywordStacks = new();

    private Stack<SqlKeyword?> GetKeywordStack(ISqlParserState state)
    {
        return _keywordStacks.GetValue(state, _ => new Stack<SqlKeyword?>());
    }

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
            // If the user injects an entity directly after an AS keyword (e.g., `AS {{ol}}`)
            if (value is ISqlEntityBase entityAlias)
            {
                if (entityAlias.Reference is ISqlReference reference)
                {
                    // If it doesn't have an alias yet, assign the fallback (e.g. "OrderLine")
                    if (string.IsNullOrWhiteSpace(reference.Alias))
                    {
                        reference.Alias = reference.FallbackAlias;
                        reference.IsAliasQuoted = true; 
                    }
                }
                
                context.ParserState.LastAliasableTarget = null;
                
                // WYSIWYG FIX: Even if consumed as an alias, if we are in a DML context, 
                // this entity MUST become the ActiveEntityTarget so DTO properties can map to it!
                string? currentKeyword = context.ParserState.CurrentKeyword?.Value;
                if (string.Equals(currentKeyword, SqlKeyword.Update, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(currentKeyword, SqlKeyword.Insert, StringComparison.OrdinalIgnoreCase))
                {
                    context.ParserState.ActiveEntityTarget = entityAlias;
                }
                
                // DEFER RENDERING! Store the AST node to be evaluated later 
                // so it perfectly picks up any late-bound mutations!
                return new SqlSegment(SqlSegmentType.Raw, entityAlias, SqlRenderMode.AliasOnly);
            }

            string? rawAlias = null;
            SqlSegment? segmentToReturn = null;
            bool isQuoted = true; // Interpolated variables used as aliases are programmatic, so safely quote them!

            if (value is string stringAlias)
            {
                rawAlias = stringAlias;
                segmentToReturn = new SqlSegment(SqlSegmentType.Literal, context.Dialect.QuoteIdentifier(stringAlias));
            }
            else if (value is ISqlProjection projAlias)
            {
                rawAlias = context.Dialect.UnquoteIdentifier(projAlias.ToSql(context, SqlRenderMode.AliasOnly));
                segmentToReturn = new SqlSegment(SqlSegmentType.Projection, projAlias, SqlRenderMode.AliasOnly); // DEFER!
            }
            else if (value is ISqlFragment fragmentAlias)
            {
                var renderedOutput = fragmentAlias.ToSql(context);
                rawAlias = context.Dialect.UnquoteIdentifier(renderedOutput);
                segmentToReturn = new SqlSegment(SqlSegmentType.Raw, fragmentAlias, SqlRenderMode.AliasOnly); // DEFER!
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
        // Explicitly guard against ISqlProjection AND ISqlQuery so subqueries retain WYSIWYG indentation!
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

                    foreach (var kvp in meta.Columns)
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

    // --- Keyword Detection Infrastructure ---

    private enum KeywordMatchMode { EndsWithWord, ContainsWord, EqualsWord }

    private readonly record struct KeywordRule(string Text, KeywordMatchMode Mode, string? Tag, SqlKeyword? ForcedKeyword);

    // Ordering IS priority — multi-word patterns must come before their single-word suffixes.
    private static readonly KeywordRule[] _keywordRules =
    [
        // --- ADDED DDL SCHEMA MODIFIERS ---
        new(SqlKeyword.Create.Value,                   KeywordMatchMode.ContainsWord, SqlSegmentTag.CreateKeyword,        SqlKeyword.Create),
        new(SqlKeyword.Drop.Value,                     KeywordMatchMode.ContainsWord, SqlSegmentTag.DropKeyword,          SqlKeyword.Drop),
        new(SqlKeyword.Alter.Value,                    KeywordMatchMode.ContainsWord, SqlSegmentTag.AlterKeyword,         SqlKeyword.Alter),
        new(SqlKeyword.Truncate.Value,                 KeywordMatchMode.ContainsWord, SqlSegmentTag.TruncateKeyword,      SqlKeyword.Truncate),
        new(SqlKeyword.Delete.Value,                   KeywordMatchMode.ContainsWord, SqlSegmentTag.DeleteKeyword,        SqlKeyword.Delete),

        new(SqlKeyword.ForUpdate.Value,                KeywordMatchMode.ContainsWord, SqlSegmentTag.ForUpdateKeyword,     null),
        new(SqlKeyword.ForShare.Value,                 KeywordMatchMode.ContainsWord, SqlSegmentTag.ForShareKeyword,      null),
        new(SqlKeyword.Limit.Value,                    KeywordMatchMode.ContainsWord, SqlSegmentTag.Paging,               null),
        new(SqlKeyword.DoUpdateSet.Value,              KeywordMatchMode.EndsWithWord, SqlSegmentTag.DoUpdateSetKeyword,   SqlKeyword.Set),   // before Set
        new(SqlKeyword.OnConflict.Value,               KeywordMatchMode.EndsWithWord, SqlSegmentTag.OnConflictKeyword,    null),
        new(SqlKeyword.Update.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.UpdateKeyword,        SqlKeyword.Update),
        new(SqlKeyword.Set.Value,                      KeywordMatchMode.EndsWithWord, SqlSegmentTag.SetKeyword,           SqlKeyword.Set),
        
        // --- UPDATED INSERT TO MAP NATIVELY ---
        new($"{SqlKeyword.Insert.Value} {SqlKeyword.Into.Value}",      KeywordMatchMode.ContainsWord, SqlSegmentTag.InsertKeyword,        SqlKeyword.Insert), 
        new(SqlKeyword.Insert.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.InsertKeyword,        SqlKeyword.Insert),
        
        new(SqlKeyword.Values.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.InsertValuesKeyword,  SqlKeyword.Values),
        new(SqlKeyword.Into.Value,                     KeywordMatchMode.ContainsWord, SqlSegmentTag.IntoKeyword,          SqlKeyword.Into),
        new(SqlKeyword.Returning.Value,                KeywordMatchMode.EndsWithWord, SqlSegmentTag.ReturningKeyword,     null),
        new(SqlKeyword.SelectDistinct.Value,           KeywordMatchMode.EndsWithWord, SqlSegmentTag.SelectDistinctKeyword, SqlKeyword.SelectDistinct), // before Select
        new(SqlKeyword.Select.Value,                   KeywordMatchMode.EndsWithWord, SqlSegmentTag.SelectKeyword,        SqlKeyword.Select),
        new(SqlKeyword.From.Value,                     KeywordMatchMode.EndsWithWord, SqlSegmentTag.FromKeyword,          SqlKeyword.From),
        new(SqlKeyword.Except.Value,                   KeywordMatchMode.EqualsWord,   SqlSegmentTag.ExceptKeyword,        null),
        new(SqlKeyword.Intersect.Value,                KeywordMatchMode.EqualsWord,   SqlSegmentTag.IntersectKeyword,     null),
        new(SqlKeyword.UnionAll.Value,                 KeywordMatchMode.EqualsWord,   SqlSegmentTag.UnionAllKeyword,      null), // before Union
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

        // WYSIWYG FIX: Do not let naive keyword scanning override the 
        // precise keyword state if we are trapped inside a subquery!
        if (forcedKeyword != null && context.ParserState.ParenDepth == 0)
        {
            context.ParserState.CurrentKeyword = forcedKeyword;
        }

        if (rented != null) ArrayPool<char>.Shared.Return(rented);

        return tag;
    }

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

            // 1. String State
            if (c == '\'' && !inBlockComment && !inLineComment)
            {
                if (inString && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    result.Append("''");
                    i++;
                    continue;
                }
                inString = !inString;
                result.Append(c);
                continue;
            }

            if (inString)
            {
                result.Append(c);
                continue;
            }

            // 2. Block Comment State
            if (!inLineComment && c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                inBlockComment = true;
                result.Append("/*");
                i++; continue;
            }
            if (inBlockComment && c == '*' && i + 1 < sql.Length && sql[i + 1] == '/')
            {
                inBlockComment = false;
                result.Append("*/");
                i++; continue;
            }
            if (inBlockComment)
            {
                result.Append(c);
                continue;
            }

            // 3. Line Comment State
            if (!inBlockComment && c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                inLineComment = true;
                result.Append("--");
                i++; continue;
            }
            if (inLineComment && (c == '\n' || c == '\r'))
            {
                inLineComment = false;
                result.Append(c);
                continue;
            }
            if (inLineComment)
            {
                result.Append(c);
                continue;
            }

            // 4. Safe Keyword Replacement (Only triggers if outside strings/comments!)
            if (i + keywordLen <= sql.Length && 
                string.Compare(sql, i, keyword, 0, keywordLen, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // Ensure word boundaries so we don't replace "Exceptional"
                bool leftSafe = i == 0 || (!char.IsLetterOrDigit(sql[i - 1]) && sql[i - 1] != '_');
                bool rightSafe = i + keywordLen == sql.Length || (!char.IsLetterOrDigit(sql[i + keywordLen]) && sql[i + keywordLen] != '_');

                if (leftSafe && rightSafe)
                {
                    result.Append(replacement);
                    i += keywordLen - 1; // Skip the original keyword
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
                isQuoted = true; // Target was explicitly quoted by the user
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
            isQuoted = false; // Target was unquoted raw text
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
        // Guard Check: If the literal ends with a targeted vertical override block,
        // do not let the forward sliding window loop overwrite our forced keyword context!
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

            // WYSIWYG FIX: Seamlessly push/pop the semantic context for subqueries!
            if (span[i] == '(')
            {
                context.ParserState.ParenDepth++;
                stack.Push(context.ParserState.CurrentKeyword);
            }
            else if (span[i] == ')')
            {
                context.ParserState.ParenDepth--;
                if (stack.Count > 0)
                {
                    // Restore outer parent's keyword state automatically!
                    context.ParserState.CurrentKeyword = stack.Pop();
                }
            }

            // Evaluate state natively even inside parentheses so subqueries map their columns properly
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

                // If not an initiator, natively detect FROM to safely exit the SELECT state!
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