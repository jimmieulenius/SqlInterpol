using SqlInterpol.Config;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

public abstract class SqlDialectBase : ISqlDialect
{
    // Specific dialects define these symbols
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

        return $"{QuoteIdentifier(source)} {SqlKeyword.As.Value} {QuoteIdentifier(alias)}";
    }

    public virtual string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        return fragment switch
        {
            SqlPagingFragment p => $"{SqlKeyword.Limit} {p.Limit} {SqlKeyword.Offset} {p.Offset}",

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

            if (segment.Tag == SqlSegmentTag.ReturningKeyword)
            {
                forceBaseNamePhase = true;
            }
            // 1. Safe detection and auto-injection for ON CONFLICT
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
            // 2. Safe detection and auto-injection for DO UPDATE SET
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
                case SqlSegmentTag.UpdateSetKeyword:
                    if (TryRewriteKeywordFragment<SqlSetFragment>(SqlKeyword.Set, segment, i)) continue;
                    break;
            }

            rewritten.Add(segment);
        }

        return rewritten;
    }

    public virtual SqlInterpolOptions GetDefaultOptions() => new();
}