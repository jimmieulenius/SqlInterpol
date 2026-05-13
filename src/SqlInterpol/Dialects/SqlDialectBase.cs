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
        "IN", "EXISTS", "ANY", "ALL", "SOME"
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
            SqlPagingFragment p => $"LIMIT {p.Limit} OFFSET {p.Offset}",

            _ => throw new NotSupportedException($"The fragment type '{fragment.GetType().Name}' is not supported by {this.GetType().Name}.")
        };
    }

    public virtual IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var rewritten = new List<SqlSegment>(segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Tag == SqlSegmentTag.InsertValuesKeyword)
            {
                // Check if the VERY NEXT segment is our DTO Fragment
                if (i + 1 < segments.Count && segments[i + 1].Value is SqlInsertValuesFragment)
                {
                    // Extract the text and find the exact start of the "VALUES" keyword
                    if (segment.Value is string text)
                    {
                        int index = text.LastIndexOf(SqlKeyword.Values, StringComparison.OrdinalIgnoreCase);
                        if (index > -1)
                        {
                            // Slice to preserve all formatting (newlines/spaces) BEFORE the word
                            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..index]));
                            continue;
                        }
                    }

                    // Safe fallback just in case
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " "));
                    continue; 
                }
            }

            rewritten.Add(segment);
        }

        return rewritten;
    }

    public virtual SqlInterpolOptions GetDefaultOptions() => new();
}