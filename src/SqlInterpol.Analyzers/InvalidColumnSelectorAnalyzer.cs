using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InvalidColumnSelectorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SQLI006",
        title: "Invalid Column Selector Expression",
        messageFormat: "Column selectors must be simple property accesses (e.g., x => x.PropertyName). Complex expressions or method calls are not supported.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error, // This should be an error because it will 100% crash at runtime
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        // Hook into indexer accesses: e.g., p[...]
        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
    }

    private void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;
        
        // Ensure they passed exactly one argument (the lambda)
        if (elementAccess.ArgumentList.Arguments.Count != 1) return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(elementAccess, context.CancellationToken);
        if (symbolInfo.Symbol is not IPropertySymbol propertySymbol) return;

        // Ensure this is the indexer on ISqlEntityBase<T>
        if (!propertySymbol.ContainingType.AllInterfaces.Any(i => i.Name == "ISqlEntityBase")) return;

        var argumentExpression = elementAccess.ArgumentList.Arguments[0].Expression;

        // Strip away the lambda (x => ...)
        if (argumentExpression is LambdaExpressionSyntax lambda)
        {
            var body = lambda.Body;

            // If the body is NOT a SimpleMemberAccess (like x.Id), flag it!
            if (body is not MemberAccessExpressionSyntax memberAccess || 
                memberAccess.Kind() != SyntaxKind.SimpleMemberAccessExpression)
            {
                var diagnostic = Diagnostic.Create(Rule, body.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}