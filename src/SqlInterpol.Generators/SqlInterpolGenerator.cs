using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

[Generator(LanguageNames.CSharp)]
public class SqlInterpolGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Create the syntax provider pipeline
        IncrementalValuesProvider<SqlExtractedQueryInfo> queryInfos = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsTargetInvocation,
                transform: ExtractQueryInfo)
            .Where(static info => info is not null)!;

        // Phase B & C will hook into this queryInfos provider.
        // context.RegisterSourceOutput(queryInfos, (spc, queryInfo) => EmitCode(spc, queryInfo));
    }

    /// <summary>
    /// PREDICATE: Extremely fast, purely syntactic check.
    /// Runs on every keystroke. Do not use SemanticModel here.
    /// </summary>
    private static bool IsTargetInvocation(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            
            // Fast filter for our target method names
            return methodName is "Append" or "Query";
        }

        return false;
    }

    /// <summary>
    /// TRANSFORM: Slower, semantic check. Runs only on nodes that passed the predicate.
    /// Extracts the interpolated string and formats it for the ANTLR parser.
    /// </summary>
    private static SqlExtractedQueryInfo? ExtractQueryInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        var methodName = memberAccess.Name.Identifier.Text;

        // Ensure we have arguments
        if (invocation.ArgumentList.Arguments.Count == 0) 
            return null;

        // Verify the argument is an interpolated string: $"""...""" or $"..."
        var argument = invocation.ArgumentList.Arguments[0].Expression;
        if (argument is not InterpolatedStringExpressionSyntax interpolatedString) 
            return null;

        // [Optional]: Validate via SemanticModel that the caller is actually our ORM type
        // var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken);
        // if (symbolInfo.Symbol is IMethodSymbol methodSymbol && 
        //     methodSymbol.ContainingType.Name != "SqlBuilder") { return null; }

        var sqlBuilder = new StringBuilder();
        var parameters = new List<SqlQueryParameterInfo>();
        int paramCount = 1;

        // Traverse the interpolated string parts
        foreach (var content in interpolatedString.Contents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (content is InterpolatedStringTextSyntax textSyntax)
            {
                // Literal string part
                sqlBuilder.Append(textSyntax.TextToken.ValueText);
            }
            else if (content is InterpolationSyntax interpolation)
            {
                // The hole: {someVar} -> $1
                sqlBuilder.Append($"${paramCount}");

                // Grab type information for the expression inside the hole
                var typeInfo = context.SemanticModel.GetTypeInfo(interpolation.Expression, cancellationToken);
                var typeName = typeInfo.Type?.ToDisplayString() ?? "object";

                parameters.Add(new SqlQueryParameterInfo(
                    Index: paramCount,
                    TypeName: typeName,
                    OriginalExpression: interpolation.Expression.ToString()
                ));

                paramCount++;
            }
        }

        return new SqlExtractedQueryInfo(
            MethodName: methodName,
            SqlTemplate: sqlBuilder.ToString(),
            Parameters: new EquatableArray<SqlQueryParameterInfo>(parameters.Select(p => new SqlQueryParameterInfo(p.Index, p.TypeName, p.OriginalExpression)).ToArray())
        );
    }
}