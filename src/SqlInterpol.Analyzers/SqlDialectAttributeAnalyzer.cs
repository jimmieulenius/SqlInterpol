using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlDialectAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SQLIA11";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Missing SqlDialect Attribute",
        messageFormat: "The dialect '{0}' implements ISqlDialect but is missing the required [SqlDialect] attribute. This attribute is required for AOT compatibility.",
        category: "Validation",
        defaultSeverity: DiagnosticSeverity.Error, 
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register a CompilationStartAction. This runs once when the build starts.
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // 1. Resolve the Type Symbols natively from the compilation!
            var dialectInterface = compilationContext.Compilation.GetTypeByMetadataName(KnownTypeNames.ISqlDialect);
            var dialectAttribute = compilationContext.Compilation.GetTypeByMetadataName(KnownTypeNames.SqlDialectAttribute);

            // If the user's project doesn't reference SqlInterpol at all, these will be null. 
            // We can safely abort analyzing this compilation entirely.
            if (dialectInterface == null || dialectAttribute == null)
            {
                return;
            }

            // 2. Register the Symbol action, closing over the resolved symbols
            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var namedTypeSymbol = (INamedTypeSymbol)symbolContext.Symbol;

                // Ignore interfaces and abstract classes
                if (namedTypeSymbol.IsAbstract || namedTypeSymbol.TypeKind == TypeKind.Interface)
                    return;

                // 3. Type-safe, string-free interface check!
                bool implementsDialect = namedTypeSymbol.AllInterfaces
                    .Any(i => SymbolEqualityComparer.Default.Equals(i, dialectInterface));

                if (implementsDialect)
                {
                    // 4. Type-safe, string-free attribute check!
                    bool hasAttribute = namedTypeSymbol.GetAttributes()
                        .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, dialectAttribute));

                    if (!hasAttribute)
                    {
                        var location = namedTypeSymbol.Locations.FirstOrDefault() ?? Location.None;
                        var diagnostic = Diagnostic.Create(Rule, location, namedTypeSymbol.Name);
                        symbolContext.ReportDiagnostic(diagnostic);
                    }
                }
            }, SymbolKind.NamedType);
        });
    }
}