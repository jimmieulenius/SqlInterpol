using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

[Generator]
public partial class SqlAotInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Unified Configuration Provider for MSBuild Properties
        var configProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
            {
                bool isDisabled = false;
                if (options.GlobalOptions.TryGetValue(GeneratorConstants.MsBuildDisableAot, out var disableAotString)
                    && bool.TryParse(disableAotString, out var disableAot))
                {
                    isDisabled = disableAot;
                }

                ImmutableArray<string> dialects = [GeneratorConstants.DefaultDialect];
                if (options.GlobalOptions.TryGetValue(GeneratorConstants.MsBuildDialects, out var dialectsString)
                     && !string.IsNullOrWhiteSpace(dialectsString))
                {
                    dialects = [.. dialectsString.Split(',', ';').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d))];
                }

                return (Dialects: dialects, IsDisabled: isDisabled);
            });

        var queryContextProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) =>
            {
                if (node is not InvocationExpressionSyntax invocation) return false;
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var name = memberAccess.Name.Identifier.Text;
                    return name is GeneratorConstants.MethodAppend or GeneratorConstants.MethodAppendLine;
                }
                return false;
            },
            transform: static (ctx, _) =>
            {
                var containingMethod = ctx.Node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (containingMethod == null) return null;

                bool hasAdvancedComposition = containingMethod.DescendantNodes()
                    .OfType<MemberAccessExpressionSyntax>()
                    .Any(ma => ma.Name.Identifier.Text == GeneratorConstants.MethodTemplate);

                if (hasAdvancedComposition) return null;

                var walker = new SqlAotSyntaxWalker(ctx.SemanticModel);
                walker.Visit(containingMethod);
                return walker.CurrentContext.AppendCalls.Count > 0 ? walker.CurrentContext : null;
            })
            .Where(static ctx => ctx != null)
            .Select(static (ctx, _) => ctx!); // FIX CS8620: Changes stream type to non-nullable context elements

        var compilationProvider = queryContextProvider.Collect().Combine(configProvider).Combine(context.CompilationProvider);

        context.RegisterSourceOutput(compilationProvider, static (spc, source) =>
        {
            var ((contexts, config), compilation) = source;

            if (config.IsDisabled)
            {
                var diagnostic = Diagnostic.Create(
                    SqlAotDiagnostics.AotDisabledDiagnostic,
                    Location.None);
                
                spc.ReportDiagnostic(diagnostic);
                return;
            }

            if (contexts.IsEmpty) return;

            Emit(spc, contexts, config.Dialects, compilation);
        });
    }
}