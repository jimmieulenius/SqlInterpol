using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnintentionalSystemMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SQLI007",
        title: "Unintentional System Method Call inside SQL Interpolation",
        messageFormat: "Do not call '{0}()' on a SQL framework object inside a query string. Pass the object directly (e.g., {{{1}}}) so the engine can parse its AST.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error, // Error, because this will 100% generate invalid SQL at runtime
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        // Hook into all method calls
        context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;

        // 1. Is it a method call like entity.ToString()?
        if (invocationExpr.Expression is not MemberAccessExpressionSyntax memberAccess) return;

        var methodName = memberAccess.Name.Identifier.Text;
        
        // 2. Is it one of the standard noisy system methods?
        if (methodName != "ToString" && 
            methodName != "GetType" && 
            methodName != "GetHashCode" && 
            methodName != "Equals")
        {
            return;
        }

        // 3. Are we actually inside a string interpolation hole like $"{...}"?
        bool isInsideInterpolatedString = invocationExpr.Ancestors().OfType<InterpolationSyntax>().Any();
        if (!isInsideInterpolatedString) return;

        // 4. Semantic Check: What type is this method being called on?
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) return;

        var containingType = methodSymbol.ContainingType;

        // Check if the object implements any of your core framework interfaces
        bool isFrameworkType = containingType.Name == "ISqlFragment" || 
                               containingType.Name == "ISqlEntityBase" ||
                               containingType.AllInterfaces.Any(i => 
                                   i.Name == "ISqlFragment" || 
                                   i.Name == "ISqlEntityBase");

        if (isFrameworkType)
        {
            // Extract the variable name they called it on to give a helpful error message
            var variableName = memberAccess.Expression.ToString();

            var diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation(), methodName, variableName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}