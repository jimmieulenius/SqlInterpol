using System.Collections.Generic;
using System.Collections.Immutable;
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
        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
    }

    private void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;

        // Must have exactly one string literal argument
        if (elementAccess.ArgumentList.Arguments.Count != 1) return;
        var argExpr = elementAccess.ArgumentList.Arguments[0].Expression;
        if (!argExpr.IsKind(SyntaxKind.StringLiteralExpression)) return;

        // Verify the indexer resolves to ISqlEntityBase.this[string] (non-generic overload)
        var symbolInfo = context.SemanticModel.GetSymbolInfo(elementAccess, context.CancellationToken);
        if (symbolInfo.Symbol is not IPropertySymbol indexerSymbol) return;
        if (indexerSymbol.Parameters.Length != 1) return;
        if (indexerSymbol.Parameters[0].Type.SpecialType != SpecialType.System_String) return;
        if (indexerSymbol.ContainingType.Name != "ISqlEntityBase") return;
        if (indexerSymbol.ContainingType.TypeArguments.Length != 0) return; // must be the non-generic interface

        // Extract T from ISqlEntityBase<T> on the receiver's type
        var receiverType = context.SemanticModel.GetTypeInfo(elementAccess.Expression, context.CancellationToken).Type;
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

        // Build the set of valid column names for T:
        // column name = [SqlColumn("name")].Name ?? PropertyName  (mirrors SqlMetadataRegistry)
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
