using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

/// <summary>
/// Abstract base class for all SQL dialect implementations, providing ANSI-compatible
/// identifier quoting, parameter naming, expression context detection, and multi-pass
/// segment rewriting for DML transformations.
/// </summary>
public abstract class SqlDialectBase : ISqlDialect
{
    /// <inheritdoc />
    public abstract SqlDialectKind Kind { get; }
    /// <inheritdoc />
    public abstract string OpenQuote { get; }
    /// <inheritdoc />
    public abstract string CloseQuote { get; }
    /// <inheritdoc />
    public abstract string ParameterPrefix { get; }

    /// <summary>
    /// SQL comparison and arithmetic operator symbols used to detect expression context
    /// before a parenthesized subquery.
    /// </summary>
    protected static readonly string[] DefaultExpressionSymbols = 
    [
        "=", "<", ">", "<=", ">=", "<>", "!=", "+", "-", "*", "/", "%"
    ];

    /// <summary>
    /// SQL keyword operators (IN, EXISTS, ANY, ALL, SOME) that indicate expression context
    /// before a parenthesized subquery.
    /// </summary>
    protected static readonly string[] DefaultExpressionKeywords = 
    [
        SqlKeyword.In,
        SqlKeyword.Exists,
        SqlKeyword.Any,
        SqlKeyword.All,
        SqlKeyword.Some
    ];

    /// <inheritdoc />
    public virtual IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>();

    /// <inheritdoc />
    public virtual int QueryParametersMaxCount => 999;

    /// <inheritdoc />
    public virtual string QuoteIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var trimmed = name.Trim();

        if (trimmed.Length < 2 || 
            !trimmed.StartsWith(OpenQuote) || 
            !trimmed.EndsWith(CloseQuote))
        {
            return $"{OpenQuote}{trimmed}{CloseQuote}";
        }

        return trimmed;
    }

    /// <inheritdoc />
    public virtual string UnquoteIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        string open = OpenQuote;
        string close = CloseQuote;

        if (identifier.StartsWith(open) && identifier.EndsWith(close))
        {
            return identifier.Substring(open.Length, identifier.Length - open.Length - close.Length);
        }

        return identifier;
    }

    /// <inheritdoc />
    public virtual string QuoteEntityName(string table, string? schema = null)
    {
        var quotedTable = QuoteIdentifier(table);

        if (string.IsNullOrWhiteSpace(schema))
        {
            return quotedTable;
        }
        
        return $"{QuoteIdentifier(schema)}.{quotedTable}";
    }

    /// <inheritdoc />
    public virtual string GetParameterName(int index)
    {
        return $"{ParameterPrefix}{index}";
    }

    /// <inheritdoc />
    public virtual bool IsExpressionContext(string textBeforeParen)
    {
        if (string.IsNullOrWhiteSpace(textBeforeParen)) 
            return false;

        foreach (var symbol in DefaultExpressionSymbols)
        {
            if (textBeforeParen.EndsWith(symbol)) return true;
        }

        int lastSeparator = textBeforeParen.LastIndexOfAny([' ', '\t', '\n', '\r', '(']);
        string lastWord = lastSeparator >= 0 
            ? textBeforeParen[(lastSeparator + 1)..] 
            : textBeforeParen;

        foreach (var keyword in DefaultExpressionKeywords)
        {
            if (lastWord.Equals(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    /// <inheritdoc />
    public string ApplyAlias(string source, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return source;
        }

        return $"{QuoteIdentifier(source)} {SqlKeyword.As.Value} {alias}";
    }

    /// <inheritdoc />
    public virtual string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        return fragment switch
        {
            SqlPagingFragment p => $"{SqlKeyword.Limit} {p.Limit} {SqlKeyword.Offset} {p.Offset}",
            SqlSetOperationFragment setOp => RenderSetOperation(setOp, context),
            SqlMultiTableUpdateFragment update => RenderMultiTableUpdate(update, context),
            SqlMultiTableDeleteFragment delete => RenderMultiTableDelete(delete, context),
            SqlSelectIntoFragment selectInto => RenderSelectInto(selectInto, context),
            _ => throw new NotSupportedException($"The fragment type '{fragment.GetType().Name}' is not supported by {this.GetType().Name}.")
        };
    }

    /// <inheritdoc />
    public virtual IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        bool forceBaseNamePhase = false;
        
        int parenDepth = 0;
        bool inString = false, inLineComment = false, inBlockComment = false;

        bool TryRewriteKeywordFragment<T>(string keyword, SqlSegment segment, int index) where T : ISqlFragment
        {
            if (index + 1 < segments.Count && segments[index + 1].Value is T)
            {
                if (segment.Value is string text)
                {
                    int keywordIndex = text.LastIndexOf(keyword, StringComparison.OrdinalIgnoreCase);

                    if (keywordIndex > -1)
                    {
                        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..keywordIndex]));
                        return true;
                    }
                }

                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " "));

                return true; 
            }
            return false;
        }

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // 1. SCAN LITERALS FOR STATEMENT BOUNDARIES (;)
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string litText)
            {
                for (int j = 0; j < litText.Length; j++)
                {
                    char c = litText[j];
                    
                    if (c == '\'' && !inBlockComment && !inLineComment) 
                    { 
                        if (inString && j + 1 < litText.Length && litText[j + 1] == '\'') { j++; continue; }
                        inString = !inString; 
                        continue; 
                    }
                    if (inString) continue;

                    if (!inLineComment && c == '/' && j + 1 < litText.Length && litText[j + 1] == '*') { inBlockComment = true; j++; continue; }
                    if (inBlockComment && c == '*' && j + 1 < litText.Length && litText[j + 1] == '/') { inBlockComment = false; j++; continue; }
                    if (inBlockComment) continue;

                    if (!inBlockComment && c == '-' && j + 1 < litText.Length && litText[j + 1] == '-') { inLineComment = true; j++; continue; }
                    if (inLineComment && (c == '\n' || c == '\r')) { inLineComment = false; continue; }
                    if (inLineComment) continue;

                    if (c == '(') parenDepth++;
                    else if (c == ')') parenDepth--;
                    else if (c == ';' && parenDepth == 0)
                    {
                        forceBaseNamePhase = false; 
                    }
                }
            }

            // 2. CHECK KEYWORD METADATA TO TOGGLE BASE-NAME STRIPPING
            var keywordMeta = SqlKeyword.AllKeywords.FirstOrDefault(k => 
                segment.Tag != null && segment.Tag.StartsWith(k.Value, StringComparison.OrdinalIgnoreCase));

            // If the keyword explicitly expects a state (true or false), apply it instantly!
            if (keywordMeta?.ExpectsBaseName.HasValue == true)
            {
                forceBaseNamePhase = keywordMeta.ExpectsBaseName.Value;
            }
            
            if (segment.Tag == SqlSegmentTag.OnConflictKeyword || 
                (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase)))
            {
                forceBaseNamePhase = true;
                if (segment.Value is string text)
                {
                    string clean = text;

                    if (segment.Tag == SqlSegmentTag.OnConflictKeyword)
                    {
                        clean = text.EndsWith(" ") ? text[..^1] : text;
                        if (!clean.EndsWith('(')) clean += " (";
                    }

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean));
                    continue;
                }
            }
            else if (segment.Tag == SqlSegmentTag.DoUpdateSetKeyword || 
                    (segment.Type == SqlSegmentType.Literal && segment.Value is string s2 && s2.Contains("DO UPDATE SET", StringComparison.OrdinalIgnoreCase)))
            {
                forceBaseNamePhase = false; 
                if (segment.Value is string text)
                {
                    string clean = text;

                    if (segment.Tag == SqlSegmentTag.DoUpdateSetKeyword)
                    {
                        if (!clean.TrimStart().StartsWith(')')) clean = ")\n" + clean.TrimStart();
                    }
                    
                    int keywordIndex = clean.LastIndexOf(SqlKeyword.Set, StringComparison.OrdinalIgnoreCase);
                    if (keywordIndex > -1 && i + 1 < segments.Count && segments[i + 1].Value is SqlSetFragment)
                    {
                        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean[..keywordIndex]));
                        continue;
                    }
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean));
                    continue;
                }
            }
            else if (segment.Tag == SqlSegmentTag.ForUpdateKeyword && segment.Value is string updateText)
            {
                int idx = updateText.IndexOf("FOR UPDATE", StringComparison.OrdinalIgnoreCase);
                if (idx > -1)
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, updateText[..idx].TrimEnd(' ', '\t')));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlLockFragment(SqlLockMode.Update)));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, updateText[(idx + 10)..]));
                    continue;
                }
            }
            else if (segment.Tag == SqlSegmentTag.ForShareKeyword && segment.Value is string shareText)
            {
                int idx = shareText.IndexOf("FOR SHARE", StringComparison.OrdinalIgnoreCase);
                if (idx > -1)
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, shareText[..idx].TrimEnd(' ', '\t')));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlLockFragment(SqlLockMode.Share)));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, shareText[(idx + 9)..]));
                    continue;
                }
            }
            else if (segment.Tag == SqlSegmentTag.ExceptKeyword || 
                segment.Tag == SqlSegmentTag.IntersectKeyword ||
                segment.Tag == SqlSegmentTag.UnionKeyword ||
                segment.Tag == SqlSegmentTag.UnionAllKeyword)
            {
                var op = segment.Tag switch
                {
                    SqlSegmentTag.ExceptKeyword => SqlSetOperator.Except,
                    SqlSegmentTag.IntersectKeyword => SqlSetOperator.Intersect,
                    SqlSegmentTag.UnionAllKeyword => SqlSetOperator.UnionAll,
                    _ => SqlSetOperator.Union
                };

                if (rewritten.Count > 0 && rewritten[^1].Value is ISqlFragment left && 
                    i + 1 < segments.Count && segments[i + 1].Value is ISqlFragment right)
                {
                    var fragment = new SqlSetOperationFragment(left, right, op);
                    rewritten[^1] = new SqlSegment(SqlSegmentType.Raw, fragment);
                    i++; 
                    continue; 
                }
            }

            // 3. APPLY THE BASE-NAME MODE
            if (forceBaseNamePhase)
            {
                if (segment.Type == SqlSegmentType.Projection && segment.Value is ISqlProjection proj)
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Projection, proj, SqlRenderMode.BaseName));
                    continue;
                }
                if (segment.Type == SqlSegmentType.Reference && segment.Value is ISqlEntityBase entity)
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Reference, entity, SqlRenderMode.BaseName));
                    continue;
                }
            }

            switch (segment.Tag)
            {
                case SqlSegmentTag.InsertValuesKeyword:
                    if (TryRewriteKeywordFragment<SqlInsertValuesFragment>(SqlKeyword.Values, segment, i)) continue;
                    break;
                case SqlSegmentTag.SetKeyword:
                    if (TryRewriteKeywordFragment<SqlSetFragment>(SqlKeyword.Set, segment, i)) continue;
                    break;
                case SqlSegmentTag.SelectKeyword:
                    if (TryRewriteKeywordFragment<SqlSelectFragment>(SqlKeyword.Select, segment, i)) continue;
                    break;
                case SqlSegmentTag.SelectDistinctKeyword:
                    if (TryRewriteKeywordFragment<SqlSelectFragment>($"{SqlKeyword.Select} {SqlKeyword.Distinct}", segment, i)) continue;
                    break;
            }

            rewritten.Add(segment);
        }

        int intoIdx = -1;
        
        for (int i = 0; i < rewritten.Count; i++)
        {
            if (rewritten[i].Tag == SqlSegmentTag.SelectIntoKeyword)
            {
                intoIdx = i;
                break;
            }
        }

        if (intoIdx >= 0)
        {
            int selectIdx = -1;
            for (int i = intoIdx - 1; i >= 0; i--)
            {
                if (rewritten[i].Tag == SqlSegmentTag.SelectKeyword || rewritten[i].Tag == SqlSegmentTag.SelectDistinctKeyword)
                {
                    selectIdx = i;
                    break;
                }
            }

            if (selectIdx >= 0)
            {
                if (!SupportedFeatures.Contains(SqlFeature.SelectInto))
                {
                    throw new SqlDialectException("'SELECT INTO' is not supported by this dialect. Use an explicit CREATE TABLE and INSERT statement.");
                }

                var intoSegment = rewritten[intoIdx];
                var text = intoSegment.Value as string ?? "";
                
                int keywordPos = text.IndexOf("INTO", StringComparison.OrdinalIgnoreCase);
                string leftOfInto = keywordPos >= 0 ? text[..keywordPos] : "";
                string rightOfInto = keywordPos >= 0 ? text[(keywordPos + 4)..] : text;
                
                object? targetTable = null;
                SqlSegment? targetParamSegment = null;

                var rightTrimmed = rightOfInto.TrimStart();
                if (rightTrimmed.Length > 0 && rightTrimmed[0] != '\n' && rightTrimmed[0] != '\r') 
                {
                    int endOfTarget = 0;
                    while (endOfTarget < rightTrimmed.Length && !char.IsWhiteSpace(rightTrimmed[endOfTarget])) endOfTarget++;
                    
                    targetTable = rightTrimmed[..endOfTarget];
                    rightOfInto = rightTrimmed[endOfTarget..];
                }
                else if (intoIdx + 1 < rewritten.Count && rewritten[intoIdx + 1].Type != SqlSegmentType.Literal)
                {
                    targetParamSegment = rewritten[intoIdx + 1];
                    targetTable = targetParamSegment.Value ?? targetParamSegment;
                }

                if (targetTable != null)
                {
                    var sourceSegments = new List<SqlSegment>();
                    
                    for(int i = 0; i <= selectIdx; i++) sourceSegments.Add(rewritten[i]);
                    for(int i = selectIdx + 1; i < intoIdx; i++) sourceSegments.Add(rewritten[i]);
                    
                    if (!string.IsNullOrWhiteSpace(leftOfInto)) 
                        sourceSegments.Add(new SqlSegment(SqlSegmentType.Literal, leftOfInto));
                        
                    int intoInsertionIndex = sourceSegments.Count;
                        
                    if (!string.IsNullOrWhiteSpace(rightOfInto))
                        sourceSegments.Add(new SqlSegment(SqlSegmentType.Literal, rightOfInto));
                        
                    int startRest = targetParamSegment != null ? intoIdx + 2 : intoIdx + 1;
                    for(int i = startRest; i < rewritten.Count; i++) sourceSegments.Add(rewritten[i]);
                    
                    var fragment = new SqlSelectIntoFragment(targetParamSegment ?? targetTable, sourceSegments, intoInsertionIndex);
                    
                    rewritten.Clear();
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, fragment));
                    return rewritten;
                }
            }
        }

        int updateIdx = -1, setIdx = -1, fromIdx = -1, whereIdx = -1;
        int firstFromIdx = -1, secondFromIdx = -1, whereDeleteIdx = -1;
        bool isDelete = false;
        
        parenDepth = 0;
        inString = false;
        inLineComment = false; 
        inBlockComment = false;

        for (int i = 0; i < rewritten.Count; i++)
        {
            var segment = rewritten[i];
            
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string litText)
            {
                for (int j = 0; j < litText.Length; j++)
                {
                    char c = litText[j];
                    
                    if (c == '\'' && !inBlockComment && !inLineComment) 
                    { 
                        if (inString && j + 1 < litText.Length && litText[j + 1] == '\'') { j++; continue; }
                        inString = !inString; 
                        continue; 
                    }
                    if (inString) continue;

                    if (!inLineComment && c == '/' && j + 1 < litText.Length && litText[j + 1] == '*') { inBlockComment = true; j++; continue; }
                    if (inBlockComment && c == '*' && j + 1 < litText.Length && litText[j + 1] == '/') { inBlockComment = false; j++; continue; }
                    if (inBlockComment) continue;

                    if (!inBlockComment && c == '-' && j + 1 < litText.Length && litText[j + 1] == '-') { inLineComment = true; j++; continue; }
                    if (inLineComment && (c == '\n' || c == '\r')) { inLineComment = false; continue; }
                    if (inLineComment) continue;

                    if (c == '(') parenDepth++;
                    else if (c == ')') parenDepth--;
                }

                if (!isDelete && parenDepth == 0 && System.Text.RegularExpressions.Regex.IsMatch(litText, @"\bDELETE\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    isDelete = true;
                }
            }

            // Only capture structural tags when we are safely in the outermost query!
            if (parenDepth == 0)
            {
                if (segment.Tag == SqlSegmentTag.UpdateKeyword && updateIdx == -1) updateIdx = i;
                else if (segment.Tag == SqlSegmentTag.SetKeyword && setIdx == -1) setIdx = i;
                
                else if (segment.Tag == SqlSegmentTag.DeleteKeyword)
                {
                    isDelete = true;
                    if (firstFromIdx == -1) firstFromIdx = i;
                }
                
                if (segment.Tag == SqlSegmentTag.FromKeyword)
                {
                    // For UPDATEs
                    if (fromIdx == -1) fromIdx = i;
                    
                    // For DELETEs
                    if (firstFromIdx == -1) firstFromIdx = i;
                    else if (secondFromIdx == -1) secondFromIdx = i;
                }
                else if (segment.Tag == SqlSegmentTag.WhereKeyword)
                {
                    if (whereIdx == -1) whereIdx = i;
                    if (whereDeleteIdx == -1) whereDeleteIdx = i;
                }
            }
        }

        if (updateIdx >= 0 && setIdx > updateIdx)
        {
            var target = new SqlSegmentCollectionFragment([.. rewritten.Skip(updateIdx + 1).Take(setIdx - updateIdx - 1)]);
            var endOfSet = fromIdx > 0 ? fromIdx : (whereIdx > 0 ? whereIdx : rewritten.Count);
            
            var setClauseSegments = rewritten.Skip(setIdx + 1).Take(endOfSet - setIdx - 1).Select(s => 
                s.Type == SqlSegmentType.Projection && s.Value is ISqlProjection p
                    ? new SqlSegment(SqlSegmentType.Projection, p, SqlRenderMode.BaseName)
                    : s
            ).ToList();
            
            var setClause = new SqlSegmentCollectionFragment(setClauseSegments);

            SqlSegmentCollectionFragment? fromClause = null;
            if (fromIdx > 0 && fromIdx > setIdx) // Ensure FROM comes after SET
            {
                int endOfFrom = whereIdx > 0 ? whereIdx : rewritten.Count;
                fromClause = new SqlSegmentCollectionFragment([.. rewritten.Skip(fromIdx + 1).Take(endOfFrom - fromIdx - 1)]);
            }

            SqlSegmentCollectionFragment? whereClause = null;
            if (whereIdx > 0)
            {
                whereClause = new SqlSegmentCollectionFragment([.. rewritten.Skip(whereIdx + 1)]);
            }

            if (fromClause != null)
            {
                rewritten.Clear();
                rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlMultiTableUpdateFragment(target, setClause, fromClause, whereClause)));
                return rewritten;
            }
        }

        if (isDelete && firstFromIdx > -1 && secondFromIdx > -1)
        {
            var targetSegments = rewritten.Skip(firstFromIdx + 1).Take(secondFromIdx - firstFromIdx - 1).ToList();
            var fromSegments = whereDeleteIdx > -1 
                ? rewritten.Skip(secondFromIdx + 1).Take(whereDeleteIdx - secondFromIdx - 1).ToList()
                : rewritten.Skip(secondFromIdx + 1).ToList();
            
            var whereSegments = whereDeleteIdx > -1 ? rewritten.Skip(whereDeleteIdx + 1).ToList() : null;

            var targetFrag = new SqlSegmentCollectionFragment(targetSegments);
            var fromFrag = new SqlSegmentCollectionFragment(fromSegments);
            var whereFrag = whereSegments != null ? new SqlSegmentCollectionFragment(whereSegments) : null;

            rewritten.Clear();
            rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlMultiTableDeleteFragment(targetFrag, fromFrag, whereFrag)));
            return rewritten;
        }

        return rewritten;
    }

    /// <inheritdoc />
    public virtual SqlInterpolOptions GetDefaultOptions() => new();

    /// <summary>Renders a set operation (UNION, EXCEPT, INTERSECT) combining two query fragments.</summary>
    protected virtual string RenderSetOperation(SqlSetOperationFragment fragment, ISqlContext context)
    {
        string opKeyword = fragment.Operator switch
        {
            SqlSetOperator.Except => SqlKeyword.Except,
            SqlSetOperator.Intersect => SqlKeyword.Intersect,
            SqlSetOperator.Union => SqlKeyword.Union,
            SqlSetOperator.UnionAll => SqlKeyword.UnionAll,
            _ => throw new NotImplementedException($"Set operator {fragment.Operator} is not supported.")
        };
        
        return $"{fragment.Left.ToSql(context)}{Environment.NewLine}{opKeyword}{Environment.NewLine}{fragment.Right.ToSql(context)}";
    }

    /// <summary>Renders the default multi-table UPDATE as <c>UPDATE ... SET ... FROM ... WHERE ...</c>.</summary>
    protected virtual string RenderMultiTableUpdate(SqlMultiTableUpdateFragment update, ISqlContext context)
    {
        var sql = $"UPDATE {update.Target.ToSql(context)}{Environment.NewLine}SET {update.SetClause.ToSql(context)}";
        
        if (update.FromClause != null) 
            sql += $"{Environment.NewLine}FROM {update.FromClause.ToSql(context)}";
            
        if (update.WhereClause != null) 
            sql += $"{Environment.NewLine}WHERE {update.WhereClause.ToSql(context)}";
            
        return sql;
    }

    /// <summary>Renders the default multi-table DELETE as <c>DELETE FROM ... FROM ... WHERE ...</c>.</summary>
    protected virtual string RenderMultiTableDelete(SqlMultiTableDeleteFragment delete, ISqlContext context)
    {
        var sql = $"DELETE FROM {delete.Target.ToSql(context)}";
        
        if (delete.FromClause != null) 
            sql += $"{Environment.NewLine}FROM {delete.FromClause.ToSql(context)}";
            
        if (delete.WhereClause != null) 
            sql += $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context)}";
            
        return sql;
    }

    /// <summary>Renders a SELECT INTO fragment, injecting <c>INTO [target]</c> at the correct segment position.</summary>
    protected virtual string RenderSelectInto(SqlSelectIntoFragment fragment, ISqlContext context)
    {
        string target = fragment.TargetTable switch
        {
            string s => QuoteIdentifier(s),
            SqlSegment paramSeg => SqlSegmentRenderer.Instance.Render(context, paramSeg, 0, [paramSeg]) ?? "",
            ISqlFragment frag => frag.ToSql(context),
            _ => fragment.TargetTable.ToString()!
        };

        var vsb = new System.Text.StringBuilder();

        for (int i = 0; i < fragment.SourceSegments.Count; i++)
        {
            if (i == fragment.IntoSegmentIndex)
            {
                vsb.Append($"{Environment.NewLine}INTO {target}");
            }

            var seg = fragment.SourceSegments[i];
            vsb.Append(SqlSegmentRenderer.Instance.Render(context, seg, i, fragment.SourceSegments));
        }

        if (fragment.IntoSegmentIndex >= fragment.SourceSegments.Count)
        {
            vsb.Append($"{Environment.NewLine}INTO {target}");
        }

        return vsb.ToString();
    }
}