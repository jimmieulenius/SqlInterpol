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

    // Expanded ANSI SQL + Major Dialects (Postgres, T-SQL, MySQL, Oracle, Snowflake, SQLite, etc.)
    private static readonly string[] KnownKeywords =
    [
        // DML / Query Structure
        "SELECT", "FROM", "WHERE", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN",
        "LIKE", "ILIKE", "IS", "NULL", "ORDER", "GROUP", "HAVING", "UNION", "ALL",
        "DISTINCT", "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE",
        "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "FULL", "CROSS", "LATERAL",
        "ON", "AS", "WITH", "CASE", "WHEN", "THEN", "ELSE", "END",
        "CAST", "COALESCE", "NULLIF", "OVER", "PARTITION", "BY",
        "COUNT", "SUM", "AVG", "MIN", "MAX",
        "LIMIT", "OFFSET", "FETCH", "ROWS", "ONLY", "FIRST", "NEXT",
        "RETURNING", "CONFLICT", "EXCLUDED", "OUTPUT", "TOP",
        "FOR", "SHARE", "LOCK", "NOCHECK", "CHECK",
        "ROW_NUMBER", "RANK", "DENSE_RANK", "LEAD", "LAG", "NTILE",
        "ASC", "DESC", "NULLS", "LAST", "ROLLUP", "CUBE", "GROUPING", "SETS",
        "TRUNCATE", "MERGE", "USING", "MATCHED",
        
        // DDL & Schema Objects
        "CREATE", "ALTER", "DROP", "RENAME", "COMMENT", "INDEX", "VIEW", "TABLE", 
        "DATABASE", "SCHEMA", "PROCEDURE", "FUNCTION", "TRIGGER", "SEQUENCE", 
        "TYPE", "DOMAIN", "ROLE", "USER", "GRANT", "REVOKE", "CASCADE", "RESTRICT",
        "PRIMARY", "FOREIGN", "UNIQUE", "DEFAULT", "CONSTRAINT", "REFERENCES", 
        "TEMPORARY", "TEMP", "UNLOGGED", "IF",
        
        // Transaction & Session Control
        "DECLARE", "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "CALL", "EXECUTE", "EXEC", 
        "PREPARE", "DEALLOCATE", "ISOLATION", "LEVEL", "READ", "WRITE", "COMMITTED", 
        "UNCOMMITTED", "REPEATABLE", "SERIALIZABLE",
        
        // Database Management & Utilities
        "EXPLAIN", "ANALYZE", "SHOW", "DESCRIBE", "PRAGMA", "COPY", "UNLOAD", 
        "VACUUM", "CLUSTER", "OPTIMIZE", "ATTACH", "DETACH",

        // Extended Dialect Keywords
        "APPLY", "PIVOT", "UNPIVOT", "TRY_CAST", "TRY_CONVERT", // T-SQL
        "REPLACE", "IGNORE", "STRAIGHT_JOIN", "DUPLICATE", "KEY", // MySQL
        "MINUS", "ROWNUM", "SYSDATE", "CONNECT", "PRIOR", "START", // Oracle
        "WINDOW", "FILTER", "WITHIN", "TABLESAMPLE", "MATERIALIZED", // Postgres/ANSI
        "FLATTEN", "UNNEST", "STRUCT", "ARRAY", "QUALIFY", "EXCLUDE", // Snowflake/BigQuery
        
        // Advanced Analytics & Aggregates
        "CUME_DIST", "PERCENT_RANK", "PERCENTILE_CONT", "PERCENTILE_DISC", "NTH_VALUE", 
        "ANY_VALUE", "APPROX_COUNT_DISTINCT", "LISTAGG", "STRING_AGG", "GROUP_CONCAT",

        // Control Flow & Procedural (T-SQL, PL/pgSQL, PL/SQL)
        "WHILE", "LOOP", "CURSOR", "BREAK", "CONTINUE", "RETURN", "GOTO", 
        "PRINT", "RAISE", "EXCEPTION", "TRY", "CATCH", "THROW", "EXIT", "LEAVE",
        
        // Common & Dialect-Specific Data types
        "INT", "INTEGER", "BIGINT", "SMALLINT", "TINYINT",
        "DECIMAL", "NUMERIC", "FLOAT", "REAL", "DOUBLE", "MONEY",
        "CHAR", "VARCHAR", "NCHAR", "NVARCHAR", "TEXT", "STRING",
        "DATE", "TIME", "DATETIME", "DATETIME2", "TIMESTAMP", "INTERVAL",
        "BOOLEAN", "BOOL", "BIT",
        "BLOB", "BINARY", "VARBINARY", "BYTEA",
        "JSON", "JSONB", "XML", "UUID", "UNIQUEIDENTIFIER",
        "SERIAL", "IDENTITY", "AUTO_INCREMENT", "AUTOINCREMENT",
        "VARCHAR2", "NVARCHAR2", "CLOB", "NCLOB", "BFILE", "RAW", "LONG", // Oracle
        "ROWID", "UROWID", "SQL_VARIANT", "HIERARCHYID", "GEOMETRY", "GEOGRAPHY", // T-SQL/Oracle
        "INET", "CIDR", "MACADDR", "MACADDR8", "TSVECTOR", "TSQUERY", "ENUM", "YEAR" // Postgres/MySQL
    ];

    private static readonly HashSet<string> KeywordSet = new(KnownKeywords, StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, HashSet<string>> ConstrainedSuccessors =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            // Upgraded FOR valid successors to support Postgres and T-SQL
            // Postgres: FOR UPDATE, FOR SHARE, FOR KEY SHARE, FOR NO KEY UPDATE
            // T-SQL: FOR XML, FOR JSON, FOR SYSTEM_TIME
            ["FOR"] = new HashSet<string>(StringComparer.Ordinal) { "KEY", "NO", "XML", "JSON", "SYSTEM_TIME", "PATH", "AUTO", "UPDATE", "SHARE" },
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

    private static IEnumerable<(string Original, string Normalized, int Offset)> ExtractWordTokens(string text)
    {
        int i = 0;
        bool skipNext = false; 
        bool afterDot = false; 

        while (i < text.Length)
        {
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

            if (i + 1 < text.Length && text[i] == '-' && text[i + 1] == '-')
            {
                afterDot = false;
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }

            if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
            {
                afterDot = false;
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
                i = Math.Min(i + 2, text.Length);
                continue;
            }

            if (text[i] == '.')
            {
                afterDot = true;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }

            if (char.IsLetter(text[i]))
            {
                int start = i;
                while (i < text.Length && char.IsLetter(text[i])) i++;

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

            afterDot = false;
            i++;
        }
    }

    private static string? FindClosestKeyword(string word)
    {
        if (word.Length < 4) return null; 

        string? best = null;
        int bestDist = 2; 

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

                if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
            }
        }

        return d[m, n];
    }
}