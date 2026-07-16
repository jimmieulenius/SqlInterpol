using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlTemplateMisuseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SQLIA07";
    private const string Category = "Performance";

    private static readonly LocalizableString Title = "Template initialized on execution path";
    private static readonly LocalizableString MessageFormat = "Templates initialized on the execution path allocate memory. Move this Template to a static readonly field, or use standard db.Append() to utilize the AOT compiler.";
    private static readonly LocalizableString Description = "SqlBuilder.Template() evaluates the $ string and allocates an AST. To get zero-allocation performance, it must be cached statically.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, 
        Title, 
        MessageFormat, 
        Category, 
        DiagnosticSeverity.Warning, 
        isEnabledByDefault: true, 
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        // Performance configurations for the analyzer
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Listen for method invocations
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Fast-path text check before doing heavy semantic analysis
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Name.Identifier.Text != "Template")
                return;
        }
        else if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            if (identifier.Identifier.Text != "Template")
                return;
        }
        else
        {
            return;
        }

        // Verify this is actually our SqlBuilder.Template method
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol == null || 
            (methodSymbol.ContainingType.Name != "SqlBuilder" && methodSymbol.ContainingType.Name != "SqlBuilderExtensions"))
        {
            return;
        }

        // 🌟 FIX: Filter specifically for MemberDeclarationSyntax so we can access .Modifiers
        var enclosingMember = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault(a => 
            a is MethodDeclarationSyntax || 
            a is ConstructorDeclarationSyntax || 
            a is PropertyDeclarationSyntax || 
            a is FieldDeclarationSyntax);

        if (enclosingMember == null) return;

        bool isStaticContext = false;

        // Check if the enclosing member is marked 'static'
        if (enclosingMember.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            isStaticContext = true;
        }
        // Exception: Allow [GlobalSetup] for BenchmarkDotNet users!
        else if (enclosingMember is MethodDeclarationSyntax methodDecl)
        {
            if (methodDecl.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().Contains("GlobalSetup")))
            {
                isStaticContext = true;
            }
        }

        // If it's not in a static context or a benchmark setup, throw the warning!
        if (!isStaticContext)
        {
            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}