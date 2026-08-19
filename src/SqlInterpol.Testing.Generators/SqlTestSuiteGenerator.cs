using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SqlInterpol.Testing.Generators;

/// <summary>
/// An incremental Roslyn source generator that reads classes implementing a suite contract interface.
/// It maps that interface to a template class (via the <c>[SqlTestSuite(typeof(IContract))]</c> attribute 
/// on the template) and emits a partial class containing xUnit <c>[Theory]</c> methods for every 
/// <c>[SqlTest]</c>-annotated method in the template.
/// </summary>
/// <remarks>
/// The generation-first paradigm eliminates runtime reflection for test discovery.
/// Each emitted <c>[Theory]</c> method is bound at compile time to a <c>[MemberData]</c>
/// property declared on the implementing class, making the test suite fully
/// Native AOT-compatible and IDE-friendly.
/// <para>
/// Template class source files are provided to this generator via <c>&lt;AdditionalFiles&gt;</c>
/// MSBuild items (pointing to the <c>SqlInterpol.Testing.Specifications</c> project directory),
/// because the templates live in a separate referenced assembly and their syntax trees are not
/// part of the consuming project's compilation.
/// </para>
/// </remarks>
[Generator]
public sealed class SqlTestSuiteGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find any class in the dialect project that implements at least one interface.
        // We cast a wide net syntactically and filter against our reverse-lookup map in the Execute phase.
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c
                                               && c.BaseList?.Types.Count > 0,
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static target => target.HasValue)
            .Select(static (target, _) => target!.Value);

        // Step 2: Parse AdditionalFiles (.cs) into lightweight, equatable template records.
        // We now parse EVERY class declaration (even partials without attributes) so we can merge them later.
        var parsedTemplates = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cs"))
            .Select(static (file, ct) => ParseTemplateFile(file, ct))
            .Where(static template => template is not null)
            .Collect();

        var combined = classDeclarations.Combine(parsedTemplates);
        context.RegisterSourceOutput(
            combined,
            static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a single additional-file into a lightweight <see cref="ParsedFile"/> record.
    /// Extracts all class declarations and their members as strings so partial classes can be
    /// merged during the execution phase.
    /// </summary>
    private static ParsedFile? ParseTemplateFile(AdditionalText file, System.Threading.CancellationToken cancellationToken)
    {
        var text = file.GetText(cancellationToken)?.ToString();
        if (string.IsNullOrEmpty(text)) return null;

        var tree = CSharpSyntaxTree.ParseText(text!, cancellationToken: cancellationToken);
        var root = tree.GetRoot(cancellationToken) as CompilationUnitSyntax;
        if (root is null) return null;

        var usings = root.Usings
            .Select(u => u.ToFullString().Trim())
            .ToImmutableArray();

        var partials = new List<ParsedPartialClass>();

        foreach (var c in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var attr = c.AttributeLists.SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() is "SqlTestSuite" or "SqlTestSuiteAttribute");

            string? interfaceName = null;
            if (attr?.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeOfExpr)
            {
                // Unpack the interface name from typeof(I...)
                interfaceName = typeOfExpr.Type switch
                {
                    IdentifierNameSyntax id => id.Identifier.Text,
                    QualifiedNameSyntax qn => qn.Right.Identifier.Text,
                    _ => typeOfExpr.Type.ToString()
                };
            }

            // Capture the full raw text of every member inside this partial class declaration
            var members = c.Members.Select(m => m.ToFullString()).ToImmutableArray();
            partials.Add(new ParsedPartialClass(c.Identifier.Text, interfaceName, members));
        }

        return partials.Count == 0 ? null : new ParsedFile(usings, partials.ToImmutableArray());
    }

    /// <summary>
    /// Filters for valid classes that implement at least one interface.
    /// </summary>
    private static (ClassDeclarationSyntax ClassSyntax, INamedTypeSymbol ClassSymbol)? GetSemanticTarget(
        GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol classSymbol)
            return null;

        if (classSymbol.AllInterfaces.Length == 0)
            return null;

        return (classSyntax, classSymbol);
    }

    /// <summary>
    /// Cross-references candidate classes against the parsed templates, merging partial templates
    /// across files and emitting a partial class for any matching suite contracts they implement.
    /// </summary>
    private static void Execute(
        SourceProductionContext context,
        (ClassDeclarationSyntax ClassSyntax, INamedTypeSymbol ClassSymbol) target,
        ImmutableArray<ParsedFile?> templates)
    {
        var className = target.ClassSymbol.Name;
        var namespaceName = target.ClassSymbol.ContainingNamespace.ToDisplayString();
        var rawFilePath = target.ClassSymbol.Locations.FirstOrDefault()?.SourceTree?.FilePath;
        var normalizedPath = rawFilePath?.Replace('\\', '/');

        // 1. Identify candidate suite interfaces.
        var candidateInterfaces = new List<INamedTypeSymbol>();
        
        foreach (var iface in target.ClassSymbol.AllInterfaces)
        {
            if (iface.Name == GeneratorConstants.SqlTestSuiteBaseInterfaceName) 
                continue;

            bool inheritsBase = iface.AllInterfaces.Any(bi => bi.Name == GeneratorConstants.SqlTestSuiteBaseInterfaceName);
            
            bool mappedInTemplate = false;
            foreach (var template in templates)
            {
                if (template is null) continue;
                if (template.Partials.Any(tc => tc.InterfaceName == iface.Name))
                {
                    mappedInTemplate = true;
                    break;
                }
            }

            if (inheritsBase || mappedInTemplate)
            {
                candidateInterfaces.Add(iface);
            }
        }

        if (candidateInterfaces.Count == 0) return;

        // 2. Process each candidate
        foreach (var contract in candidateInterfaces)
        {
            // --- ENFORCEMENT 1: Must inherit ISqlTestSuiteBase ---
            bool inheritsBase = contract.AllInterfaces.Any(bi => bi.Name == GeneratorConstants.SqlTestSuiteBaseInterfaceName);
            if (!inheritsBase)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SqlTestingDiagnostics.InvalidSuiteContractDiagnostic,
                    target.ClassSyntax.GetLocation(),
                    contract.Name));
                
                continue; 
            }

            // Find the name of the template class mapping to this contract
            string? templateClassName = null;
            foreach (var template in templates)
            {
                if (template is null) continue;
                var match = template.Partials.FirstOrDefault(p => p.InterfaceName == contract.Name);
                if (match is not null)
                {
                    templateClassName = match.Name;
                    break;
                }
            }

            // --- ENFORCEMENT 2: Template must exist ---
            if (templateClassName is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SqlTestingDiagnostics.MissingTemplateDiagnostic,
                    target.ClassSyntax.GetLocation(),
                    className,
                    contract.Name));
                
                continue; 
            }

            // 3. Aggregate all partial declarations of this template class across all files
            var allMemberTexts = new List<string>();
            var templateUsings = new List<string>();

            foreach (var template in templates)
            {
                if (template is null) continue;
                
                var partialsForClass = template.Partials.Where(p => p.Name == templateClassName).ToList();
                if (partialsForClass.Count > 0)
                {
                    templateUsings.AddRange(template.Usings);
                    foreach (var pc in partialsForClass)
                    {
                        allMemberTexts.AddRange(pc.MemberTexts);
                    }
                }
            }

            // 4. Reconstruct a single unified class for Roslyn to parse so we can easily iterate its members
            var mergedSource = new StringBuilder();
            mergedSource.AppendLine($"class {templateClassName} {{");
            foreach(var m in allMemberTexts) 
            {
                mergedSource.AppendLine(m);
            }
            mergedSource.AppendLine("}");

            var reparsedTree = CSharpSyntaxTree.ParseText(mergedSource.ToString());
            var templateClassSyntax = reparsedTree.GetRoot().DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == templateClassName);

            if (templateClassSyntax is null) continue;

            // Validate the test data members exist
            ValidateTestDataMembers(context, target.ClassSyntax, contract, templateClassSyntax);

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable"); // FIX: Inject nullable context for the generated code!

            var usingsList = new List<string>
            {
                "using SqlInterpol;",
                "using SqlInterpol.Schema;",
                "using SqlInterpol.Testing.Specifications;",
                "using SqlInterpol.Testing.Xunit;",
                "using Xunit;"
            };
            usingsList.AddRange(templateUsings);

            foreach (var usingDirective in usingsList.Distinct())
                sb.AppendLine(usingDirective);

            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
            sb.AppendLine($"public partial class {className}");
            sb.Append("{");

            var activeMembers = templateClassSyntax.Members
                .Where(m => !m.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() is "SqlGeneratorIgnore" or "SqlGeneratorIgnoreAttribute"))
                .ToList();

            for (int i = 0; i < activeMembers.Count; i++)
            {
                sb.AppendLine();
                EmitMember(sb, activeMembers[i], target.ClassSymbol, normalizedPath);
                if (i < activeMembers.Count - 1) sb.AppendLine(); 
            }

            sb.AppendLine(); 
            sb.Append("}");

            // Generate a unique file name in case a class implements multiple suites
            context.AddSource($"{className}_{contract.Name}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    /// <summary>
    /// Validates that every <c>[SqlTest("X")]</c> attribute on a template method references
    /// a property <c>X</c> that is actually declared on the suite contract interface.
    /// </summary>
    private static void ValidateTestDataMembers(
        SourceProductionContext context,
        ClassDeclarationSyntax targetClassSyntax,
        INamedTypeSymbol suiteInterface,
        ClassDeclarationSyntax templateClassSyntax)
    {
        foreach (var templateMethod in templateClassSyntax.Members.OfType<MethodDeclarationSyntax>())
        {
            var sqlTestAttr = templateMethod.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() is "SqlTest" or "SqlTestAttribute");

            if (sqlTestAttr is null) continue;

            var dataPropertyName = ExtractDataPropertyName(sqlTestAttr);
            if (string.IsNullOrWhiteSpace(dataPropertyName)) continue;

            if (!suiteInterface.GetMembers(dataPropertyName!).Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SqlTestingDiagnostics.MismatchedTestDataMemberDiagnostic,
                    targetClassSyntax.GetLocation(),
                    dataPropertyName,
                    templateMethod.Identifier.Text,
                    templateClassSyntax.Identifier.Text,
                    suiteInterface.Name));
            }
        }
    }

    /// <summary>
    /// Emits a single template member into the generated partial class.
    /// Methods annotated with <c>[SqlTest]</c> are promoted to xUnit <c>[Theory]</c> methods.
    /// All other members are emitted verbatim.
    /// </summary>
    private static void EmitMember(
        StringBuilder sb,
        MemberDeclarationSyntax member,
        INamedTypeSymbol classSymbol,
        string? normalizedFilePath)
    {
        if (member is MethodDeclarationSyntax method)
        {
            var sqlTestAttr = method.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() is "SqlTest" or "SqlTestAttribute");

            if (sqlTestAttr is not null)
            {
                EmitTheoryMethod(sb, method, sqlTestAttr, classSymbol, normalizedFilePath);
                return;
            }
        }

        // Fields, nested types, and helper methods are pasted verbatim.
        sb.Append("    " + member.ToFullString().Trim());
    }

    /// <summary>
    /// Emits the xUnit <c>[Theory]</c> / <c>[MemberData]</c> wrapper for a <c>[SqlTest]</c>-annotated method.
    /// </summary>
    private static void EmitTheoryMethod(
        StringBuilder sb,
        MethodDeclarationSyntax method,
        AttributeSyntax sqlTestAttr,
        INamedTypeSymbol classSymbol,
        string? normalizedFilePath)
    {
        var dataPropertyName = ExtractDataPropertyName(sqlTestAttr);
        if (string.IsNullOrWhiteSpace(dataPropertyName)) return;

        // Look up the line number of the TheoryData property for the Ctrl+Click navigation hint.
        int lineNumber = 1;
        var dataMember = classSymbol.GetMembers(dataPropertyName!).FirstOrDefault();
        if (dataMember is not null)
        {
            var location = dataMember.Locations.FirstOrDefault();
            if (location is not null)
                lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;
        }

        if (normalizedFilePath is not null)
            sb.AppendLine($"    // Ctrl+Click to edit test data: file:///{normalizedFilePath}#{lineNumber}");

        sb.AppendLine("    [Theory]");
        sb.AppendLine($"    [MemberData(nameof({dataPropertyName}))]");

        var modifiers = method.Modifiers.ToFullString().Trim();
        if (!modifiers.Contains("public"))
            modifiers = "public " + modifiers;

        sb.AppendLine($"    {modifiers.Trim()} {method.ReturnType} {method.Identifier}{method.ParameterList}");
        sb.Append((method.Body?.ToFullString() ?? $"    {{ {method.ExpressionBody?.ToFullString()}; }}").TrimEnd());
    }

    /// <summary>
    /// Safely unwraps the raw identifier from the syntax tree, accounting for
    /// standard string literals and compiler intrinsics like <c>nameof(...)</c>.
    /// </summary>
    private static string? ExtractDataPropertyName(AttributeSyntax sqlTestAttr)
    {
        var argExpr = sqlTestAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
        if (argExpr is null) return null;

        // Handle string literal: [SqlTest("MyDataProperty")]
        if (argExpr is LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText;
        }

        // Handle compiler intrinsic: [SqlTest(nameof(ILockTestSuite.MyDataProperty))]
        if (argExpr is InvocationExpressionSyntax invocation &&
            invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" })
        {
            var nameofArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            
            // Handles `nameof(Interface.Property)`
            if (nameofArg is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.Identifier.Text;
            }
            // Handles `nameof(Property)`
            if (nameofArg is IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.Text;
            }
        }

        // Fallback for unexpected or complex syntax scenarios
        return argExpr.ToString().Trim('"', ' ');
    }

    // ── Supporting records ─────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight, equatable snapshot of a single parsed additional-file's content.
    /// Stored in the incremental pipeline to avoid re-parsing unchanged files.
    /// </summary>
    private sealed record ParsedFile(
        ImmutableArray<string> Usings,
        ImmutableArray<ParsedPartialClass> Partials);

    /// <summary>
    /// Lightweight, equatable snapshot of a single partial class declaration extracted from an additional file.
    /// </summary>
    private sealed record ParsedPartialClass(
        string Name, 
        string? InterfaceName, 
        ImmutableArray<string> MemberTexts);
}