namespace SqlInterpol;

public static class SqlOperator
{
    // Operators that don't require spaces (e.g., "Id=(")
    public static readonly string[] Symbols = 
    [
        "=", "<", ">", "<=", ">=", "<>", "!=", "+", "-", "*", "/", "%"
    ];

    // Word-based operators that DO require a word boundary (e.g., "IN (")
    public static readonly string[] Keywords = 
    [
        "IN", "EXISTS", "ANY", "ALL", "SOME"
    ];

    public static bool IsExpressionContext(string textBeforeParen)
    {
        if (string.IsNullOrWhiteSpace(textBeforeParen)) 
            return false;

        // 1. Check for symbol operators (they can touch the previous word safely)
        foreach (var symbol in Symbols)
        {
            if (textBeforeParen.EndsWith(symbol)) return true;
        }

        // 2. Check for word operators (they need to be isolated words)
        // We look for spaces, tabs, newlines, or even another open parenthesis
        int lastSeparator = textBeforeParen.LastIndexOfAny([' ', '\t', '\n', '\r', '(']);
        string lastWord = lastSeparator >= 0 
            ? textBeforeParen[(lastSeparator + 1)..] 
            : textBeforeParen;

        foreach (var keyword in Keywords)
        {
            if (lastWord.Equals(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}