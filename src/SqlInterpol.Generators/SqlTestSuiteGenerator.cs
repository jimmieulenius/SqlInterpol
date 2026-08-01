using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SqlInterpol.Generators;

/// <summary>
/// An incremental Roslyn source generator that reads classes implementing an interface
/// annotated with <c>[SqlTestSuite(typeof(TemplateClass))]</c> and emits a partial class
/// containing xUnit <c>[Theory]</c> methods for every <c>[SqlTest]</c>-annotated method
/// in the template.
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
    // Fully-qualified attribute name guards against accidental matches from other assemblies.
    private const string SqlTestSuiteAttributeFullName = "SqlInterpol.Testing.Specifications.SqlTestSuiteAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find classes whose implemented interfaces carry [SqlTestSuite(typeof(Template))].
        // The predicate is a cheap syntax-only filter; the semantic transform does the heavy lifting.
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c
                                                && c.BaseList?.Types.Count > 0,
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static target => target is not null);

        // Step 2: Parse AdditionalFiles (.cs) into lightweight, equatable template records.
        // Parsing is done here (in .Select) so the incremental pipeline can cache the result
        // and avoid re-parsing unchanged files on every keystroke.
        var parsedTemplates = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cs"))
            .Select(static (file, ct) => ParseTemplateFile(file, ct))
            .Where(static template => template is not null)
            .Collect();

        var combined = classDeclarations.Combine(parsedTemplates);
        context.RegisterSourceOutput(
            combined,
            static (spc, source) => Execute(spc, source.Left!.Value, source.Right));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a single additional-file into a lightweight <see cref="ParsedTemplate"/> record.
    /// Returns <see langword="null"/> when the file contains no class declarations.
    /// </summary>
    /// <param name="file">The additional text file to parse.</param>
    /// <param name="cancellationToken">Cancellation token forwarded from the pipeline.</param>
    /// <returns>A <see cref="ParsedTemplate"/>, or <see langword="null"/> when nothing is found.</returns>
    private static ParsedTemplate? ParseTemplateFile(AdditionalText file, System.Threading.CancellationToken cancellationToken)
    {
        var text = file.GetText(cancellationToken)?.ToString();
        if (string.IsNullOrEmpty(text)) return null;

        var tree = CSharpSyntaxTree.ParseText(text!, cancellationToken: cancellationToken);
        var root = tree.GetRoot(cancellationToken) as CompilationUnitSyntax;
        if (root is null) return null;

        var usings = root.Usings
            .Select(u => u.ToFullString().Trim())
            .ToImmutableArray();

        var classes = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Select(c => new ParsedTemplateClass(c.Identifier.Text, c.ToFullString()))
            .ToImmutableArray();

        return classes.IsEmpty ? null : new ParsedTemplate(usings, classes);
    }

    /// <summary>
    /// Inspects a class declaration to determine whether it implements a suite contract interface.
    /// </summary>
    /// <param name="context">The generator syntax context for the candidate class.</param>
    /// <returns>
    /// A generation target tuple, or <see langword="null"/> when the class does not
    /// implement any suite contract interface.
    /// </returns>
    private static (ClassDeclarationSyntax ClassSyntax, INamedTypeSymbol ClassSymbol, INamedTypeSymbol TemplateSymbol)? GetSemanticTarget(
        GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol classSymbol)
            return null;

        foreach (var iface in classSymbol.AllInterfaces)
        {
            // Use the fully-qualified attribute name to avoid false matches from other assemblies.
            var suiteAttr = iface.GetAttributes().FirstOrDefault(
                a => a.AttributeClass?.ToDisplayString() == SqlTestSuiteAttributeFullName);

            if (suiteAttr?.ConstructorArguments.FirstOrDefault().Value is not INamedTypeSymbol templateSymbol)
                continue;

            return (classSyntax, classSymbol, templateSymbol);
        }

        return null;
    }

    /// <summary>
    /// Emits a partial class for the implementing class, wiring each <c>[SqlTest]</c>
    /// template method to its corresponding xUnit <c>[Theory]</c> and <c>[MemberData]</c>.
    /// </summary>
    /// <param name="context">The Roslyn source production context.</param>
    /// <param name="target">The resolved generation target.</param>
    /// <param name="templates">All parsed template records from AdditionalFiles.</param>
    private static void Execute(
        SourceProductionContext context,
        (ClassDeclarationSyntax ClassSyntax, INamedTypeSymbol ClassSymbol, INamedTypeSymbol TemplateSymbol) target,
        ImmutableArray<ParsedTemplate?> templates)
    {
        var className = target.ClassSymbol.Name;
        var namespaceName = target.ClassSymbol.ContainingNamespace.ToDisplayString();
        var rawFilePath = target.ClassSymbol.Locations.FirstOrDefault()?.SourceTree?.FilePath;
        var normalizedPath = rawFilePath?.Replace('\\', '/');
        var templateName = target.TemplateSymbol.Name;

        // Locate the pre-parsed template class by name.
        ParsedTemplateClass? templateClass = null;
        ImmutableArray<string> templateUsings = ImmutableArray<string>.Empty;

        foreach (var template in templates)
        {
            if (template is null) continue;
            var found = template.Classes.FirstOrDefault(c => c.Name == templateName);
            if (found is not null)
            {
                templateClass = found;
                templateUsings = template.Usings;
                break;
            }
        }

        if (templateClass is null) return;

        // Re-parse the pre-extracted class text (cheap: it's already been parsed once in the pipeline).
        var reparsedTree = CSharpSyntaxTree.ParseText(templateClass.FullText);
        var classSyntax = reparsedTree.GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == templateName);

        if (classSyntax is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");

        // Collect standard and template usings, then deduplicate.
        var usingsList = new List<string>
        {
            "using SqlInterpol.Schema;",
            "using SqlInterpol.Testing.Xunit;",
            "using Xunit;"
        };

        usingsList.AddRange(templateUsings);

        foreach (var usingDirective in usingsList.Distinct())
            sb.AppendLine(usingDirective);

        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Partial class merges with the user's handwritten test data file.
        sb.AppendLine($"public partial class {className}");
        sb.AppendLine("{");

        foreach (var member in classSyntax.Members)
            EmitMember(sb, member, target.ClassSymbol, normalizedPath);

        sb.AppendLine("}");

        context.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    /// <summary>
    /// Emits a single template member into the generated partial class.
    /// Members annotated with <c>[SqlIgnoreMember]</c> are skipped.
    /// Methods annotated with <c>[SqlTest]</c> are promoted to xUnit <c>[Theory]</c> methods.
    /// All other members are emitted verbatim.
    /// </summary>
    private static void EmitMember(
        StringBuilder sb,
        MemberDeclarationSyntax member,
        INamedTypeSymbol classSymbol,
        string? normalizedFilePath)
    {
        bool hasIgnoreAttr = member.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "SqlIgnoreMember" or "SqlIgnoreMemberAttribute");

        if (hasIgnoreAttr) return;

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
        sb.AppendLine("    " + member.ToFullString().Trim());
        sb.AppendLine();
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
        var dataPropertyName = sqlTestAttr.ArgumentList?.Arguments.FirstOrDefault()?.ToString().Trim('"');
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
        sb.AppendLine(method.Body?.ToFullString() ?? $"    {{ {method.ExpressionBody?.ToFullString()}; }}");
        sb.AppendLine();
    }

    // ── Supporting records ─────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight, equatable snapshot of a single parsed additional-file's content.
    /// Stored in the incremental pipeline to avoid re-parsing unchanged files.
    /// </summary>
    private sealed record ParsedTemplate(
        ImmutableArray<string> Usings,
        ImmutableArray<ParsedTemplateClass> Classes);

    /// <summary>
    /// Lightweight, equatable snapshot of a single class declaration extracted from an additional file.
    /// </summary>
    private sealed record ParsedTemplateClass(string Name, string FullText);
}