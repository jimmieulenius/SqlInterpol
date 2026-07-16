using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringColumnMismatchAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SQLIA03",
        title: "Unknown column name on entity",
        messageFormat: "Column name '{0}' does not exist on entity '{1}'. This will produce invalid SQL.",
        category: "Correctness",
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

        // Only target specific dynamic lookup methods
        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "Column" && methodName != "OrderBy" && methodName != "OrderByDescending") return;

        if (invocation.ArgumentList.Arguments.Count == 0) return;
        var argExpr = invocation.ArgumentList.Arguments[0].Expression;
        
        // We only care if they hardcoded a string. If they pass a variable, we assume they know what they are doing.
        if (!argExpr.IsKind(SyntaxKind.StringLiteralExpression)) return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) return;

        // Make sure it's being called on an ISqlEntityBase
        if (!methodSymbol.ContainingType.AllInterfaces.Any(i => i.Name == "ISqlEntityBase")) return;

        // Extract T from ISqlEntityBase<T> on the receiver's type
        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if (receiverType == null) return;

        ITypeSymbol? modelType = null;
        foreach (var iface in receiverType.AllInterfaces)
        {
            if (iface.Name == "ISqlEntityBase" && iface.TypeArguments.Length == 1)
            {
                modelType = iface.TypeArguments[0];
                break;
            }
        }
        if (modelType == null) return;

        var validColumnNames = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var member in modelType.GetMembers())
        {
            if (member is not IPropertySymbol prop) continue;
            if (prop.DeclaredAccessibility != Accessibility.Public || prop.IsStatic) continue;
            if (HasAttribute(prop, "SqlIgnoreAttribute")) continue;

            string columnName = prop.Name;
            foreach (var attr in prop.GetAttributes())
            {
                if (attr.AttributeClass?.Name == "SqlColumnAttribute" &&
                    attr.ConstructorArguments.Length == 1 &&
                    attr.ConstructorArguments[0].Value is string attrName)
                {
                    columnName = attrName;
                    break;
                }
            }
            validColumnNames.Add(columnName);
        }

        var requestedName = (string)((LiteralExpressionSyntax)argExpr).Token.Value!;

        if (!validColumnNames.Contains(requestedName))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, argExpr.GetLocation(), requestedName, modelType.Name));
        }
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == attributeName)
                return true;
        }
        return false;
    }
}