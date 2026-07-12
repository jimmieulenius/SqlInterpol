using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

public class SqlAotSyntaxWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    public CompileTimeQueryContext CurrentContext { get; } = new();

    public SqlAotSyntaxWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;

            if (methodName == "Entity" && memberAccess.Name is GenericNameSyntax genericName)
            {
                TrackEntityDeclaration(node, genericName);
            }
            else if (methodName is "Append" or "AppendLine")
            {
                var isInsideQuery = node.Ancestors().OfType<InvocationExpressionSyntax>()
                    .Any(inv => inv.Expression is MemberAccessExpressionSyntax ma && ma.Name.Identifier.Text == "Query");

                if (!isInsideQuery)
                {
                    var symbol = _semanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
                    if (symbol != null && symbol.Parameters.Length > 0 && 
                        symbol.Parameters[0].Type.Name.Contains("SqlQueryInterpolatedStringHandler"))
                    {
                        string handlerType = symbol.Parameters[0].Type.ToDisplayString();
                        CurrentContext.AppendCalls.Add(new AppendCallContext(node, methodName, handlerType));
                    }
                }
            }
            else if (methodName == "Query")
            {
                if (node.ArgumentList.Arguments.Count > 0)
                {
                    var firstArg = node.ArgumentList.Arguments[0].Expression;
                    if (firstArg is IdentifierNameSyntax ident)
                    {
                        CurrentContext.SubqueryEntities.Add(ident.Identifier.Text);
                    }
                }
            }
        }

        base.VisitInvocationExpression(node);
    }

    private void TrackEntityDeclaration(InvocationExpressionSyntax node, GenericNameSyntax genericName)
    {
        string variableName = "";
        string? explicitAlias = null;
        string? customName = null;
        string? customSchema = null;
        bool wasAutoAliased = true;

        // 🌟 FLUENT API CONFIGURATION ANALYZER 🌟
        int positionalIndex = 0;
        foreach (var argument in node.ArgumentList.Arguments)
        {
            if (argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
            {
                if (argument.Expression is DeclarationExpressionSyntax decl)
                    variableName = decl.Designation.ToString();
                else if (argument.Expression is IdentifierNameSyntax ident)
                    variableName = ident.Identifier.Text;
                continue; 
            }

            string? stringValue = null;
            if (argument.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                stringValue = literal.Token.ValueText;
            }

            if (argument.NameColon != null)
            {
                var paramName = argument.NameColon.Name.Identifier.Text;
                if (paramName == "alias") { explicitAlias = stringValue; if (stringValue != null) wasAutoAliased = false; }
                else if (paramName == "name") { customName = stringValue; }
                else if (paramName == "schema") { customSchema = stringValue; }
            }
            else
            {
                if (positionalIndex == 0) { explicitAlias = stringValue; if (stringValue != null) wasAutoAliased = false; }
                else if (positionalIndex == 1) { customName = stringValue; }
                else if (positionalIndex == 2) { customSchema = stringValue; }
                positionalIndex++;
            }
        }

        if (string.IsNullOrEmpty(variableName)) return;

        var typeArg = genericName.TypeArgumentList.Arguments.First();
        var typeSymbol = _semanticModel.GetTypeInfo(typeArg).Type as INamedTypeSymbol;

        string typeName = typeArg.ToString();
        string mappedTableName = typeName;
        string? mappedSchemaName = null;
        var columns = new List<ColumnMap>();

        if (typeSymbol != null)
        {
            var sqlTableAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name is "SqlTableAttribute" or "SqlTable");

            if (sqlTableAttr != null)
            {
                if (sqlTableAttr.ConstructorArguments.Length > 0)
                    mappedTableName = sqlTableAttr.ConstructorArguments[0].Value?.ToString() ?? typeSymbol.Name;
                
                if (sqlTableAttr.ConstructorArguments.Length > 1)
                    mappedSchemaName = sqlTableAttr.ConstructorArguments[1].Value?.ToString();

                foreach (var namedArg in sqlTableAttr.NamedArguments)
                {
                    if (namedArg.Key == "Name" && namedArg.Value.Value != null) mappedTableName = namedArg.Value.Value.ToString();
                    if (namedArg.Key == "Schema" && namedArg.Value.Value != null) mappedSchemaName = namedArg.Value.Value.ToString();
                }
            }
            else
            {
                mappedTableName = typeSymbol.Name;
            }

            foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (member.IsStatic || member.IsIndexer) continue; // 🌟 FIXED: Typo cleanly removed!
                if (member.Name == "EqualityContract") continue;

                var attributes = member.GetAttributes();
                if (attributes.Any(a => a.AttributeClass?.Name is "SqlIgnoreAttribute" or "SqlIgnore")) continue;

                var propType = member.Type;
                if (propType.TypeKind == TypeKind.Class && 
                    propType.SpecialType != SpecialType.System_String && 
                    propType.ToDisplayString() != "byte[]")
                {
                    continue;
                }

                string colName = member.Name;
                var sqlColumnAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name is "SqlColumnAttribute" or "SqlColumn");

                if (sqlColumnAttr != null && sqlColumnAttr.ConstructorArguments.Length > 0)
                {
                    var mappedName = sqlColumnAttr.ConstructorArguments[0].Value?.ToString();
                    if (!string.IsNullOrEmpty(mappedName)) colName = mappedName;
                }
                
                columns.Add(new ColumnMap(member.Name, colName));
            }
            
            columns = columns.OrderBy(c => c.PropertyName).ToList();
        }

        if (customName != null) mappedTableName = customName;
        if (customSchema != null) mappedSchemaName = customSchema;

        var declaration = new EntityDeclaration(
            variableName, 
            typeName, 
            mappedTableName, 
            mappedSchemaName, 
            explicitAlias, 
            wasAutoAliased,
            columns);
            
        CurrentContext.Entities[variableName] = declaration;
    }
}