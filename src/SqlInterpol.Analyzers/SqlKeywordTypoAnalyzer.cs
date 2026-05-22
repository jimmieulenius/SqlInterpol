using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlConstraintAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ConstraintRule = new(
        id: "SQLI004",
        title: "Invalid SQL keyword succession",
        messageFormat: "Invalid SQL syntax: '{0}' cannot immediately follow '{1}'",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Strict, undeniable structural rules that cross all major SQL dialects.
    private static readonly IReadOnlyDictionary<string, HashSet<string>> ConstrainedSuccessors =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["GROUP"] = new(StringComparer.Ordinal) { "BY" },
            ["ORDER"] = new(StringComparer.Ordinal) { "BY" },
            ["LEFT"]  = new(StringComparer.Ordinal) { "JOIN", "OUTER" },
            ["RIGHT"] = new(StringComparer.Ordinal) { "JOIN", "OUTER" },
            ["INNER"] = new(StringComparer.Ordinal) { "JOIN" },
            ["CROSS"] = new(StringComparer.Ordinal) { "JOIN", "APPLY" },
            ["OUTER"] = new(StringComparer.Ordinal) { "JOIN", "APPLY" }
        };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConstraintRule);

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

        string? lastConstraintInitiator = null;

        foreach (var content in interpolatedString.Contents)
        {
            // CRITICAL FIX: If we hit a C# variable hole (e.g., {{dynamicClause}}),
            // we MUST reset the constraint state. The developer provided the missing 
            // keyword programmatically!
            if (content is not InterpolatedStringTextSyntax textSyntax)
            {
                lastConstraintInitiator = null;
                continue;
            }

            var text = textSyntax.TextToken.ValueText;
            foreach (var (original, normalized, _) in ExtractWordTokens(text))
            {
                // Check if the current word violates the pending constraint
                if (lastConstraintInitiator != null && ConstrainedSuccessors.TryGetValue(lastConstraintInitiator, out var validSuccessors))
                {
                    if (!validSuccessors.Contains(normalized))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            ConstraintRule, textSyntax.GetLocation(), original, lastConstraintInitiator));
                    }
                    
                    // Reset constraint state regardless of pass/fail
                    lastConstraintInitiator = null; 
                }

                // Check if the CURRENT word initiates a new constraint for the NEXT word
                if (ConstrainedSuccessors.ContainsKey(normalized))
                {
                    lastConstraintInitiator = normalized;
                }
            }
        }
    }

    private static IEnumerable<(string Original, string Normalized, int Offset)> ExtractWordTokens(string text)
    {
        int i = 0;
        bool skipNext = false; 

        while (i < text.Length)
        {
            // 1. Skip Strings
            if (text[i] == '\'')
            {
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\'' && i + 1 < text.Length && text[i + 1] == '\'') { i += 2; continue; }
                    if (text[i] == '\'') { i++; break; }
                    i++;
                }
                continue;
            }

            // 2. Skip Line Comments
            if (i + 1 < text.Length && text[i] == '-' && text[i + 1] == '-')
            {
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }

            // 3. Skip Block Comments
            if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
                i = Math.Min(i + 2, text.Length);
                continue;
            }

            // 4. Capture Words
            if (char.IsLetter(text[i]))
            {
                int start = i;
                while (i < text.Length && char.IsLetter(text[i])) i++;

                if (i < text.Length && (text[i] == '_' || char.IsDigit(text[i])))
                {
                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                    skipNext = false;
                    continue;
                }

                var original = text.Substring(start, i - start);
                var normalized = original.ToUpperInvariant();

                bool shouldSkip = skipNext;
                skipNext = (normalized == "AS");

                // CRITICAL FIX: Changed to >= 2 so 2-letter keywords like "BY" and "ON" are captured!
                if (!shouldSkip && original.Length >= 2) 
                    yield return (original, normalized, start);

                continue;
            }

            i++;
        }
    }
}