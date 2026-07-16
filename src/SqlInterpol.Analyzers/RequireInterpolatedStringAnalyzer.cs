using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequireInterpolatedStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SQLIA01",
        title: "Require Interpolated String for SqlBuilder",
        messageFormat: "Do not pass raw variables to SqlBuilder.Append. Use an interpolated string (e.g. $\"{{var}}\") so parameters are safely tracked.",
        category: "Security",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;

        if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName != "Append" && methodName != "AppendLine") return;

            if (invocationExpr.ArgumentList.Arguments.Count == 0) return;

            // === THE FIX: Semantic Model Validation ===
            // Ask the compiler for the true Type of the method being invoked
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) return;

            // Only trigger if the method belongs specifically to "SqlBuilder"!
            // if (methodSymbol.ContainingType.Name != "SqlBuilder") return;
            if (methodSymbol.ContainingType.ToDisplayString() != "SqlInterpol.SqlBuilder") return;
            // ==========================================

            var firstArgument = invocationExpr.ArgumentList.Arguments[0].Expression;

            // Flag anything that isn't an interpolated string: identifiers, raw string literals,
            // string concatenation (classic SQL injection vector), and verbatim string literals.
            if (firstArgument.IsKind(SyntaxKind.IdentifierName) ||
                firstArgument.IsKind(SyntaxKind.StringLiteralExpression) ||
                firstArgument.IsKind(SyntaxKind.AddExpression))
            {
                var diagnostic = Diagnostic.Create(Rule, firstArgument.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}