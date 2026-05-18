using SqlInterpol.Config;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

public abstract class SqlDialectBase : ISqlDialect
{
    public abstract SqlDialectKind Kind { get; }
    public abstract string OpenQuote { get; }
    public abstract string CloseQuote { get; }
    public abstract string ParameterPrefix { get; }
    protected static readonly string[] DefaultExpressionSymbols = 
    [
        "=", "<", ">", "<=", ">=", "<>", "!=", "+", "-", "*", "/", "%"
    ];

    protected static readonly string[] DefaultExpressionKeywords = 
    [
        SqlKeyword.In,
        SqlKeyword.Exists,
        SqlKeyword.Any,
        SqlKeyword.All,
        SqlKeyword.Some
    ];

    // Common logic for all dialects
    public virtual string QuoteIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var trimmed = name.Trim();

        // If the string is too short to be quoted (e.g., "[A]"), or 
        // it doesn't start/end with the dialect's quotes, add them.
        if (trimmed.Length < 2 || 
            !trimmed.StartsWith(OpenQuote) || 
            !trimmed.EndsWith(CloseQuote))
        {
            return $"{OpenQuote}{trimmed}{CloseQuote}";
        }

        return trimmed;
    }

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
            // Dynamically strip based on the length of the quote characters.
            // This safely handles single chars like '[' or multi-chars like '<<'
            return identifier.Substring(open.Length, identifier.Length - open.Length - close.Length);
        }

        return identifier;
    }

    public virtual string QuoteEntityName(string table, string? schema = null)
    {
        var quotedTable = QuoteIdentifier(table);

        if (string.IsNullOrWhiteSpace(schema))
        {
            return quotedTable;
        }
        
        return $"{QuoteIdentifier(schema)}.{quotedTable}";
    }

    public virtual string GetParameterName(int index)
    {
        // Default logic: @p0, @p1, etc.
        return $"{ParameterPrefix}{index}";
    }

    public virtual bool IsExpressionContext(string textBeforeParen)
    {
        if (string.IsNullOrWhiteSpace(textBeforeParen)) 
            return false;

        // 1. Check for symbol operators (they can touch the previous word safely)
        foreach (var symbol in DefaultExpressionSymbols)
        {
            if (textBeforeParen.EndsWith(symbol)) return true;
        }

        // 2. Check for word operators (they need to be isolated words)
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

    public string ApplyAlias(string source, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return source;
        }

        // return $"{QuoteIdentifier(source)} {SqlKeyword.As.Value} {QuoteIdentifier(alias)}";
        return $"{QuoteIdentifier(source)} {SqlKeyword.As.Value} {alias}";
    }

    public virtual string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        return fragment switch
        {
            SqlPagingFragment p => $"{SqlKeyword.Limit} {p.Limit} {SqlKeyword.Offset} {p.Offset}",

            SqlSetOperationFragment setOp => RenderSetOperation(setOp, context),
            
            SqlMultiTableUpdateFragment update => RenderMultiTableUpdate(update, context),

            SqlMultiTableDeleteFragment delete => RenderMultiTableDelete(delete, context),
            // SqlMultiTableDeleteFragment delete => 
            //     $"DELETE FROM {delete.Target.ToSql(context).Trim()}{Environment.NewLine}FROM {delete.FromClause.ToSql(context).Trim()}" +
            //     (delete.WhereClause != null ? $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context).Trim()}" : ""),

            _ => throw new NotSupportedException($"The fragment type '{fragment.GetType().Name}' is not supported by {this.GetType().Name}.")
        };
    }

    public virtual IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        bool forceBaseNamePhase = false;

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

            if (segment.Tag == SqlSegmentTag.ReturningKeyword) forceBaseNamePhase = true;
            
            else if (segment.Tag == SqlSegmentTag.OnConflictKeyword || 
                    (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase)))
            {
                forceBaseNamePhase = true;
                if (segment.Value is string text)
                {
                    string clean = text.EndsWith(" ") ? text[..^1] : text;
                    if (!clean.EndsWith('(')) clean += " (";
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
                    if (!clean.TrimStart().StartsWith(')')) clean = ")\n" + clean.TrimStart();
                    
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

            if (forceBaseNamePhase && segment.Type == SqlSegmentType.Projection && segment.Value is ISqlProjection proj)
            {
                rewritten.Add(new SqlSegment(SqlSegmentType.Projection, proj, SqlRenderMode.BaseName));
                continue;
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

        int updateIdx = -1, setIdx = -1, fromIdx = -1, whereIdx = -1;
        int firstFromIdx = -1, secondFromIdx = -1, whereDeleteIdx = -1;
        bool isDelete = false;
        
        int parenDepth = 0;
        bool inString = false, inLineComment = false, inBlockComment = false;

        for (int i = 0; i < rewritten.Count; i++)
        {
            var segment = rewritten[i];
            
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string litText)
            {
                // Safely track parenthesis depth so we don't bleed into subqueries!
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

        // 1. Evaluate Multi-Table UPDATE
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

        // 2. Evaluate Multi-Table DELETE
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

    public virtual SqlInterpolOptions GetDefaultOptions() => new();

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

    protected virtual string RenderMultiTableUpdate(SqlMultiTableUpdateFragment update, ISqlContext context)
    {
        // Standard Layout (SQL Server, Postgres, SQLite, Oracle)
        var sql = $"UPDATE {update.Target.ToSql(context)}{Environment.NewLine}SET {update.SetClause.ToSql(context)}";
        
        if (update.FromClause != null) 
            sql += $"{Environment.NewLine}FROM {update.FromClause.ToSql(context)}";
            
        if (update.WhereClause != null) 
            sql += $"{Environment.NewLine}WHERE {update.WhereClause.ToSql(context)}";
            
        return sql;
    }

    protected virtual string RenderMultiTableDelete(SqlMultiTableDeleteFragment delete, ISqlContext context)
    {
        // Standard Layout (SQL Server, Postgres, SQLite, Oracle)
        var sql = $"DELETE FROM {delete.Target.ToSql(context)}";
        
        if (delete.FromClause != null) 
            sql += $"{Environment.NewLine}FROM {delete.FromClause.ToSql(context)}";
            
        if (delete.WhereClause != null) 
            sql += $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context)}";
            
        return sql;
    }
}