using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DuplicateAliasAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SQLI008",
        title: "Duplicate SQL Alias",
        messageFormat: "The alias '{0}' is defined multiple times in this query string. This will cause a SQL execution error.",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

        var seenAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // State machine variables MUST persist outside the loop to handle interpolation holes properly!
        // E.g., it can handle: CAST({{p[x => x.Price]}} AS INT)
        int parenDepth = 0;
        var castDepths = new Stack<int>();
        bool expectingCastParen = false;
        bool nextWordIsAlias = false;
        bool inString = false;
        bool inBlockComment = false;
        bool inLineComment = false;

        foreach (var content in interpolatedString.Contents)
        {
            // We only parse the raw SQL text chunks, ignoring the C# AST holes
            if (content is not InterpolatedStringTextSyntax textSyntax) continue;

            var text = textSyntax.TextToken.ToString();
            int i = 0;

            while (i < text.Length)
            {
                // 1. Process active strings and comments
                if (inLineComment)
                {
                    if (text[i] == '\n') inLineComment = false;
                    i++; continue;
                }
                if (inBlockComment)
                {
                    if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '/')
                    {
                        inBlockComment = false;
                        i += 2;
                    }
                    else i++;
                    continue;
                }
                if (inString)
                {
                    if (text[i] == '\'')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '\'') i += 2; // Escaped quote
                        else { inString = false; i++; }
                    }
                    else i++;
                    continue;
                }

                // 2. Detect starts of strings and comments
                if (text[i] == '-' && i + 1 < text.Length && text[i + 1] == '-') { inLineComment = true; i += 2; continue; }
                if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*') { inBlockComment = true; i += 2; continue; }
                if (text[i] == '\'') { inString = true; i++; continue; }

                // 3. Track Parenthesis Depth for CAST logic
                if (text[i] == '(')
                {
                    parenDepth++;
                    if (expectingCastParen)
                    {
                        castDepths.Push(parenDepth);
                        expectingCastParen = false;
                    }
                    i++; continue;
                }
                if (text[i] == ')')
                {
                    if (castDepths.Count > 0 && castDepths.Peek() == parenDepth)
                    {
                        castDepths.Pop(); // We have exited the CAST context
                    }
                    parenDepth--;
                    i++; continue;
                }

                if (char.IsWhiteSpace(text[i]))
                {
                    i++; continue;
                }

                // 4. Read words and identifiers
                if (char.IsLetter(text[i]) || text[i] == '_' || text[i] == '[' || text[i] == '"' || text[i] == '`')
                {
                    int start = i;
                    char quote = text[i] == '[' ? ']' : (text[i] == '"' || text[i] == '`' ? text[i] : '\0');

                    if (quote != '\0')
                    {
                        i++;
                        while (i < text.Length && text[i] != quote) i++;
                        if (i < text.Length) i++; // Consume closing quote
                    }
                    else
                    {
                        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                    }

                    string word = text.Substring(start, i - start);
                    string normalized = word.Trim('[', ']', '"', '`').ToUpperInvariant();

                    if (nextWordIsAlias)
                    {
                        nextWordIsAlias = false;
                        if (!seenAliases.Add(normalized))
                        {
                            // Precision mapping for the red squiggly line!
                            var spanStart = textSyntax.SpanStart + start;
                            var location = Location.Create(context.Node.SyntaxTree, TextSpan.FromBounds(spanStart, spanStart + word.Length));
                            context.ReportDiagnostic(Diagnostic.Create(Rule, location, word));
                        }
                    }
                    else if (normalized == "CAST")
                    {
                        expectingCastParen = true;
                    }
                    else if (normalized == "AS")
                    {
                        // The magic: Only expect an alias if we are NOT inside a CAST() block!
                        if (castDepths.Count == 0)
                        {
                            nextWordIsAlias = true;
                        }
                    }
                    else
                    {
                        expectingCastParen = false;
                    }
                    continue;
                }

                expectingCastParen = false;
                i++;
            }
        }
    }
}