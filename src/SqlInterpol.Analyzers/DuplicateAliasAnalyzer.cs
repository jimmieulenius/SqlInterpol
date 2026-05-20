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
    private static readonly DiagnosticDescriptor DuplicateRule = new(
        id: "SQLI008",
        title: "Duplicate SQL Alias",
        messageFormat: "The alias '{0}' is defined multiple times in this scope. This will cause a SQL execution error.",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ShadowRule = new(
        id: "SQLI009",
        title: "Alias shadows physical column",
        messageFormat: "The explicit alias '{0}' matches an existing physical column on a queried table. Ensure this does not cause ambiguity.",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Info, // Info level, as it's legal SQL but potentially dangerous
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DuplicateRule, ShadowRule);

    private class LexicalScope
    {
        public HashSet<string> ColumnAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> TableAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> PhysicalColumns { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool InSelectClause { get; set; } = false;
    }

    private static readonly HashSet<string> ScopeResetKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "FROM", "WHERE", "GROUP", "HAVING", "ORDER", "JOIN", "ON", 
        "LIMIT", "OFFSET", "FETCH", "SET", "VALUES", "INTO", "UPDATE", "DELETE", "INSERT"
    };

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

        var scopeStack = new Stack<LexicalScope>();
        scopeStack.Push(new LexicalScope());

        int parenDepth = 0;
        var castDepths = new Stack<int>();
        bool expectingCastParen = false;
        bool nextWordIsAlias = false;
        
        bool inString = false;
        bool inBlockComment = false;
        bool inLineComment = false;

        foreach (var content in interpolatedString.Contents)
        {
            // --- 1. C# INTERPOLATION HOLE PARSING ---
            if (content is InterpolationSyntax interpolation)
            {
                var scope = scopeStack.Peek();

                // If this is a table variable (e.g. {{p}}), extract its physical C# properties!
                ExtractPhysicalColumns(interpolation, context.SemanticModel, scope.PhysicalColumns);

                if (nextWordIsAlias)
                {
                    nextWordIsAlias = false;
                    string? holeName = ExtractColumnName(interpolation);
                    
                    if (holeName != null)
                    {
                        var targetSet = scope.InSelectClause ? scope.ColumnAliases : scope.TableAliases;
                        string upperName = holeName.ToUpperInvariant();

                        if (!targetSet.Add(upperName))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(DuplicateRule, interpolation.GetLocation(), holeName));
                        }
                        else if (scope.InSelectClause && scope.PhysicalColumns.Contains(upperName))
                        {
                            // Flag that this new alias perfectly matches a physical column name
                            context.ReportDiagnostic(Diagnostic.Create(ShadowRule, interpolation.GetLocation(), holeName));
                        }
                    }
                }
                
                expectingCastParen = false;
                continue;
            }

            // --- 2. SQL RAW TEXT PARSING ---
            if (content is not InterpolatedStringTextSyntax textSyntax) continue;

            var text = textSyntax.TextToken.ToString();
            int i = 0;

            while (i < text.Length)
            {
                if (char.IsWhiteSpace(text[i])) { i++; continue; }

                if (inLineComment) { if (text[i] == '\n') inLineComment = false; i++; continue; }
                if (inBlockComment) { if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '/') { inBlockComment = false; i += 2; } else i++; continue; }
                if (inString) { if (text[i] == '\'') { if (i + 1 < text.Length && text[i + 1] == '\'') i += 2; else { inString = false; i++; } } else i++; continue; }

                if (text[i] == '-' && i + 1 < text.Length && text[i + 1] == '-') { inLineComment = true; i += 2; continue; }
                if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*') { inBlockComment = true; i += 2; continue; }
                if (text[i] == '\'') { inString = true; i++; continue; }

                if (text[i] == '(')
                {
                    parenDepth++;
                    scopeStack.Push(new LexicalScope());
                    if (expectingCastParen) { castDepths.Push(parenDepth); expectingCastParen = false; }
                    i++; continue;
                }
                if (text[i] == ')')
                {
                    if (scopeStack.Count > 1) scopeStack.Pop();
                    if (castDepths.Count > 0 && castDepths.Peek() == parenDepth) castDepths.Pop();
                    parenDepth--;
                    i++; continue;
                }

                if (char.IsLetter(text[i]) || text[i] == '_' || text[i] == '[' || text[i] == '"' || text[i] == '`')
                {
                    int start = i;
                    char quote = text[i] == '[' ? ']' : (text[i] == '"' || text[i] == '`' ? text[i] : '\0');

                    if (quote != '\0')
                    {
                        i++;
                        while (i < text.Length && text[i] != quote) i++;
                        if (i < text.Length) i++;
                    }
                    else
                    {
                        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                    }

                    string word = text.Substring(start, i - start);
                    string normalized = word.Trim('[', ']', '"', '`').ToUpperInvariant();

                    // --- STATE MACHINE UPDATES ---
                    if (normalized == "SELECT") 
                    {
                        scopeStack.Peek().InSelectClause = true;
                    }
                    else if (normalized == "UNION" || normalized == "INTERSECT" || normalized == "EXCEPT" || normalized == "MINUS")
                    {
                        // Wipe the column memory so the next SELECT block can reuse aliases safely!
                        scopeStack.Peek().InSelectClause = false;
                        scopeStack.Peek().ColumnAliases.Clear();
                    }
                    else if (ScopeResetKeywords.Contains(normalized)) 
                    {
                        scopeStack.Peek().InSelectClause = false;
                    }

                    // --- EXPLICIT ALIAS TRACKING ---
                    if (nextWordIsAlias)
                    {
                        nextWordIsAlias = false;
                        var scope = scopeStack.Peek();
                        var targetSet = scope.InSelectClause ? scope.ColumnAliases : scope.TableAliases;

                        if (!targetSet.Add(normalized))
                        {
                            var spanStart = textSyntax.SpanStart + start;
                            var location = Location.Create(context.Node.SyntaxTree, TextSpan.FromBounds(spanStart, spanStart + word.Length));
                            context.ReportDiagnostic(Diagnostic.Create(DuplicateRule, location, word));
                        }
                        else if (scope.InSelectClause && scope.PhysicalColumns.Contains(normalized))
                        {
                            var spanStart = textSyntax.SpanStart + start;
                            var location = Location.Create(context.Node.SyntaxTree, TextSpan.FromBounds(spanStart, spanStart + word.Length));
                            context.ReportDiagnostic(Diagnostic.Create(ShadowRule, location, word));
                        }
                    }
                    else if (normalized == "CAST") expectingCastParen = true;
                    else if (normalized == "AS" && castDepths.Count == 0) nextWordIsAlias = true;
                    else expectingCastParen = false;
                    
                    continue;
                }

                expectingCastParen = false;
                i++;
            }
        }
    }

    private static string? ExtractColumnName(InterpolationSyntax interpolation)
    {
        if (interpolation.Expression is ElementAccessExpressionSyntax elementAccess &&
            elementAccess.ArgumentList.Arguments.Count == 1 &&
            elementAccess.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax lambda)
        {
            if (lambda.Body is MemberAccessExpressionSyntax memberAccess)
                return memberAccess.Name.Identifier.Text;
                
            if (lambda.Body is IdentifierNameSyntax identifierName)
                return identifierName.Identifier.Text;
        }
        
        if (interpolation.Expression is LiteralExpressionSyntax literal && 
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private static void ExtractPhysicalColumns(InterpolationSyntax interpolation, SemanticModel semanticModel, HashSet<string> physicalColumns)
    {
        // Identifies variables like `{{p}}` or `{{ol}}` to extract their underlying C# class properties
        if (interpolation.Expression is IdentifierNameSyntax identifier)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(identifier);
            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                var parameterType = parameterSymbol.Type;
                
                // Assuming your table parameters are generic, e.g., Table<Product>
                if (parameterType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                {
                    var entityType = namedType.TypeArguments[0];
                    foreach (var member in entityType.GetMembers())
                    {
                        if (member is IPropertySymbol property && property.DeclaredAccessibility == Accessibility.Public)
                        {
                            physicalColumns.Add(property.Name.ToUpperInvariant());
                        }
                    }
                }
            }
        }
    }
}