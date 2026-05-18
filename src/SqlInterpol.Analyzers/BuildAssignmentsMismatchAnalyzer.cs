using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BuildAssignmentsMismatchAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SQLI002",
        title: "DTO property does not exist on entity",
        messageFormat: "Property '{0}' on the DTO does not exist on entity '{1}'. This will throw at runtime.",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
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
        var invocationExpr = (InvocationExpressionSyntax)context.Node;

        if (invocationExpr.Expression is not MemberAccessExpressionSyntax memberAccess) return;
        if (memberAccess.Name.Identifier.Text != "BuildAssignments") return;
        if (invocationExpr.ArgumentList.Arguments.Count != 2) return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) return;
        if (methodSymbol.ContainingType.ToDisplayString() != "SqlInterpol.Sql") return;

        // 1. Extract T from the entity argument's ISqlEntityBase<T>
        var entityArgExpr = invocationExpr.ArgumentList.Arguments[0].Expression;
        var entityType = context.SemanticModel.GetTypeInfo(entityArgExpr, context.CancellationToken).Type;
        if (entityType == null) return;

        ITypeSymbol? modelType = null;
        foreach (var iface in entityType.AllInterfaces)
        {
            if (iface.Name == "ISqlEntityBase" && iface.TypeArguments.Length == 1)
            {
                modelType = iface.TypeArguments[0];
                break;
            }
        }
        if (modelType == null) return;

        // 2. Collect valid property names on the entity (public, non-static, no [SqlIgnore])
        var entityPropertyNames = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var member in modelType.GetMembers())
        {
            if (member is IPropertySymbol prop &&
                prop.DeclaredAccessibility == Accessibility.Public &&
                !prop.IsStatic &&
                !HasAttribute(prop, "SqlIgnoreAttribute"))
            {
                entityPropertyNames.Add(prop.Name);
            }
        }

        // 3. Get DTO type from the second argument
        var dtoArgExpr = invocationExpr.ArgumentList.Arguments[1].Expression;
        var dtoType = context.SemanticModel.GetTypeInfo(dtoArgExpr, context.CancellationToken).Type;
        if (dtoType == null) return;

        // For anonymous object creation, build a map of property name → initializer node
        // so we can point the squiggle at the specific bad property rather than the whole expression.
        var initializerMap = new Dictionary<string, SyntaxNode>();
        if (dtoArgExpr is AnonymousObjectCreationExpressionSyntax anonCreation)
        {
            foreach (var initializer in anonCreation.Initializers)
            {
                // Named form: `Name = expr`
                if (initializer.NameEquals != null)
                {
                    initializerMap[initializer.NameEquals.Name.Identifier.Text] = initializer;
                }
                // Projection form: `someObj.Prop` — property name is the last identifier
                else if (initializer.Expression is MemberAccessExpressionSyntax proj)
                {
                    initializerMap[proj.Name.Identifier.Text] = initializer;
                }
                else if (initializer.Expression is IdentifierNameSyntax id)
                {
                    initializerMap[id.Identifier.Text] = initializer;
                }
            }
        }

        // 4. Flag each DTO property that has no matching entity property
        foreach (var member in dtoType.GetMembers())
        {
            if (member is not IPropertySymbol dtoProp) continue;
            if (dtoProp.DeclaredAccessibility != Accessibility.Public || dtoProp.IsStatic) continue;
            if (HasAttribute(dtoProp, "SqlIgnoreAttribute")) continue;

            if (!entityPropertyNames.Contains(dtoProp.Name))
            {
                var location = initializerMap.TryGetValue(dtoProp.Name, out var node)
                    ? node.GetLocation()
                    : dtoArgExpr.GetLocation();

                context.ReportDiagnostic(Diagnostic.Create(Rule, location, dtoProp.Name, modelType.Name));
            }
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
