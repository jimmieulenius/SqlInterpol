using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlKeywordTypoAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SQLI004",
        title: "Possible SQL keyword typo",
        messageFormat: "'{0}' is not a recognized SQL keyword. Did you mean '{1}'.",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConstraintRule = new(
        id: "SQLI004",
        title: "Invalid keyword after SQL keyword",
        messageFormat: "'{0}' is not a recognized SQL keyword after {1}",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Common ANSI SQL + dialect-specific keywords and data types
    private static readonly string[] KnownKeywords =
    [
        // DML / query structure
        "SELECT", "FROM", "WHERE", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN",
        "LIKE", "ILIKE", "IS", "NULL", "ORDER", "GROUP", "HAVING", "UNION", "ALL",
        "DISTINCT", "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE",
        "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "FULL", "CROSS", "LATERAL",
        "ON", "AS", "WITH", "CASE", "WHEN", "THEN", "ELSE", "END",
        "CAST", "COALESCE", "NULLIF", "OVER", "PARTITION", "BY",
        "COUNT", "SUM", "AVG", "MIN", "MAX",
        "LIMIT", "OFFSET", "FETCH", "ROWS", "ONLY", "FIRST", "NEXT",
        "RETURNING", "CONFLICT", "EXCLUDED",
        "FOR", "SHARE", "LOCK",
        "ROW_NUMBER", "RANK", "DENSE_RANK", "LEAD", "LAG", "NTILE",
        "ASC", "DESC", "NULLS", "LAST",
        "ROLLUP", "CUBE", "GROUPING", "SETS",
        "TRUNCATE", "MERGE", "USING", "MATCHED",
        // Data types
        "INT", "INTEGER", "BIGINT", "SMALLINT", "TINYINT",
        "DECIMAL", "NUMERIC", "FLOAT", "REAL", "DOUBLE",
        "CHAR", "VARCHAR", "NCHAR", "NVARCHAR", "TEXT",
        "DATE", "TIME", "DATETIME", "TIMESTAMP", "INTERVAL",
        "BOOLEAN", "BOOL", "BIT",
        "BLOB", "BINARY", "VARBINARY", "BYTEA",
        "JSON", "JSONB", "XML", "UUID",
        "SERIAL", "IDENTITY",
    ];

    private static readonly HashSet<string> KeywordSet = new(KnownKeywords, StringComparer.Ordinal);

    // After these keywords, if the next word-token is not a known keyword and not in the
    // listed valid set, it is flagged. UPDATE/SHARE/etc. are already in KeywordSet so they
    // never reach the constrained check; only non-keyword valid successors are listed here.
    private static readonly IReadOnlyDictionary<string, HashSet<string>> ConstrainedSuccessors =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            // FOR UPDATE / FOR SHARE / FOR KEY SHARE / FOR NO KEY UPDATE
            // UPDATE and SHARE are KeywordSet members; KEY and NO are not.
            ["FOR"] = new HashSet<string>(StringComparer.Ordinal) { "KEY", "NO" },
        };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule, ConstraintRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "Append" && methodName != "AppendLine") return;
        if (invocation.ArgumentList.Arguments.Count == 0) return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) return;
        if (methodSymbol.ContainingType.ToDisplayString() != "SqlInterpol.SqlBuilder") return;

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        if (firstArg is not InterpolatedStringExpressionSyntax interpolatedString) return;

        // lastKeyword persists across interpolation holes: the constrained check only fires
        // for non-keywords, so valid successors (UPDATE, SHARE, etc.) are always in KeywordSet
        // and will update lastKeyword cleanly regardless of what holes produce.
        // e.g. FOR {{"dfd"}} AAA still flags AAA; FOR {{expr}} UPDATE is fine.
        string? lastKeyword = null;
        foreach (var content in interpolatedString.Contents)
        {
            if (content is not InterpolatedStringTextSyntax textSyntax) continue;

            var text = textSyntax.TextToken.ValueText;
            foreach (var (original, normalized, offset) in ExtractWordTokens(text))
            {
                if (KeywordSet.Contains(normalized))
                {
                    lastKeyword = normalized;
                    continue;
                }

                // Constrained-successor check: e.g. after FOR, only UPDATE/SHARE/KEY/NO are valid.
                if (lastKeyword != null &&
                    ConstrainedSuccessors.TryGetValue(lastKeyword, out var validSuccessors))
                {
                    if (!validSuccessors.Contains(normalized))
                        context.ReportDiagnostic(Diagnostic.Create(
                            ConstraintRule, textSyntax.GetLocation(), original, lastKeyword));
                    lastKeyword = null;
                    continue;
                }

                lastKeyword = null;

                var suggestion = FindClosestKeyword(normalized);
                if (suggestion == null) continue;

                context.ReportDiagnostic(Diagnostic.Create(Rule, textSyntax.GetLocation(), original, suggestion));
            }
        }
    }

    // Yields (original, normalized, startIndex) for purely-alphabetic tokens (any case),
    // skipping SQL strings, comments, dot-qualified names, and tokens immediately after AS.
    private static IEnumerable<(string Original, string Normalized, int Offset)> ExtractWordTokens(string text)
    {
        int i = 0;
        bool skipNext = false; // true when next token is an alias (preceded by AS)
        bool afterDot = false; // true when previous non-whitespace char was '.'

        while (i < text.Length)
        {
            // Skip SQL string literals: '...' (with '' escape)
            if (text[i] == '\'')
            {
                afterDot = false;
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\'' && i + 1 < text.Length && text[i + 1] == '\'') { i += 2; continue; }
                    if (text[i] == '\'') { i++; break; }
                    i++;
                }
                continue;
            }

            // Skip line comments: --
            if (i + 1 < text.Length && text[i] == '-' && text[i + 1] == '-')
            {
                afterDot = false;
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }

            // Skip block comments: /* ... */
            if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
            {
                afterDot = false;
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
                i = Math.Min(i + 2, text.Length);
                continue;
            }

            // Track dot for qualified names (table.column)
            if (text[i] == '.')
            {
                afterDot = true;
                i++;
                continue;
            }

            // Whitespace does not reset afterDot — `table . column` is still qualified
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }

            // Read a purely-alphabetic token (any case)
            if (char.IsLetter(text[i]))
            {
                int start = i;
                while (i < text.Length && char.IsLetter(text[i])) i++;

                // Compound identifier (underscore or digit follows) — skip entirely
                if (i < text.Length && (text[i] == '_' || char.IsDigit(text[i])))
                {
                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                    afterDot = false;
                    skipNext = false;
                    continue;
                }

                var original = text.Substring(start, i - start);
                var normalized = original.ToUpperInvariant();

                bool shouldSkip = skipNext || afterDot;
                afterDot = false;
                skipNext = (normalized == "AS");

                if (!shouldSkip && original.Length >= 3)
                    yield return (original, normalized, start);

                continue;
            }

            // Any other character resets dot context
            afterDot = false;
            i++;
        }
    }

    private static string? FindClosestKeyword(string word)
    {
        if (word.Length < 4) return null; // too short for reliable fuzzy matching

        string? best = null;
        int bestDist = 2; // only report if distance is strictly less than 2 (i.e., == 1)

        foreach (var keyword in KnownKeywords)
        {
            if (Math.Abs(word.Length - keyword.Length) >= bestDist) continue;
            int dist = DamerauLevenshtein(word, keyword);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = keyword;
            }
        }

        return best;
    }

    // Damerau-Levenshtein distance (accounts for transpositions, e.g. FORM ↔ FROM)
    private static int DamerauLevenshtein(string s, string t)
    {
        int m = s.Length, n = t.Length;
        var d = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);

                // Transposition
                if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
            }
        }

        return d[m, n];
    }
}
