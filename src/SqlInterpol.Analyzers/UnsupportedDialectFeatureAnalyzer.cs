using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlInterpol.Analyzers;

/// <summary>
/// SQLI005 — Reports an error when a SQL feature is passed as an interpolated-string hole to
/// <c>SqlBuilder.Append</c>/<c>AppendLine</c> but the dialect chosen at construction time does
/// not support that feature (which would throw <c>SqlDialectException</c> at runtime).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnsupportedDialectFeatureAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SQLI005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "SQL feature not supported by dialect",
        messageFormat: "'{0}' is not supported by the {1} dialect and will throw at runtime",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The SQL feature used is not supported by the configured dialect. " +
                     "Use a dialect that supports this feature, or remove the feature usage.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // Mirrors each dialect's SupportedFeatures set.
    // Key: factory method name on SqlBuilder (e.g. "PostgreSql").
    // Value: set of SqlFeature member names that are supported.
    private static readonly IReadOnlyDictionary<string, ISet<string>> DialectFeatures =
        new Dictionary<string, ISet<string>>(StringComparer.Ordinal)
        {
            ["PostgreSql"] = new HashSet<string>(StringComparer.Ordinal)
                { "ForUpdate", "ForShare", "Returning", "OnConflict" },
            ["SqlServer"] = new HashSet<string>(StringComparer.Ordinal)
                { "ForUpdate", "ForShare", "Returning", "OnConflict" },
            ["MySql"] = new HashSet<string>(StringComparer.Ordinal)
                { "ForUpdate", "ForShare", "OnConflict" },
            ["SqLite"] = new HashSet<string>(StringComparer.Ordinal)
                { "Returning", "OnConflict" },
            ["Oracle"] = new HashSet<string>(StringComparer.Ordinal)
                { "ForUpdate", "Returning" },
        };

    private static readonly IReadOnlyDictionary<string, string> FeatureDisplayNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ForUpdate"]  = "FOR UPDATE",
            ["ForShare"]   = "FOR SHARE",
            ["Returning"]  = "RETURNING",
            ["OnConflict"] = "ON CONFLICT",
        };

    // Patterns for feature keywords that may appear as literal SQL text rather than holes.
    // The runtime parser detects these by segment tagging, so they throw at runtime just like
    // fragment holes do.
    private static readonly (string FeatureKey, Regex Pattern)[] TextFeaturePatterns =
    [
        ("ForUpdate",  new Regex(@"\bFOR\s+UPDATE\b", RegexOptions.IgnoreCase)),
        ("ForShare",   new Regex(@"\bFOR\s+SHARE\b",  RegexOptions.IgnoreCase)),
        ("Returning",  new Regex(@"\bRETURNING\b",     RegexOptions.IgnoreCase)),
        ("OnConflict", new Regex(@"\bON\s+CONFLICT\b", RegexOptions.IgnoreCase)),
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlock);
    }

    private static void AnalyzeCodeBlock(CodeBlockStartAnalysisContext<SyntaxKind> blockContext)
    {
        // Maps local variable name → dialect name within this block.
        var variableDialects = new Dictionary<string, string>(StringComparer.Ordinal);

        // Track: var db = SqlBuilder.PostgreSql(); (and similar)
        blockContext.RegisterSyntaxNodeAction(ctx =>
        {
            var local = (LocalDeclarationStatementSyntax)ctx.Node;
            foreach (var variable in local.Declaration.Variables)
            {
                if (variable.Initializer?.Value is not InvocationExpressionSyntax initCall) continue;
                var dialect = TryGetDialectFromFactoryCall(initCall, ctx.SemanticModel);
                if (dialect != null)
                    variableDialects[variable.Identifier.Text] = dialect;
            }
        }, SyntaxKind.LocalDeclarationStatement);

        // Track: db = SqlBuilder.PostgreSql(); (reassignment)
        blockContext.RegisterSyntaxNodeAction(ctx =>
        {
            var expr = (ExpressionStatementSyntax)ctx.Node;
            if (expr.Expression is not AssignmentExpressionSyntax assignment) return;
            if (assignment.Left is not IdentifierNameSyntax leftIdent) return;
            if (assignment.Right is not InvocationExpressionSyntax initCall) return;
            var dialect = TryGetDialectFromFactoryCall(initCall, ctx.SemanticModel);
            if (dialect != null)
                variableDialects[leftIdent.Identifier.Text] = dialect;
        }, SyntaxKind.ExpressionStatement);

        // Check each Append/AppendLine call for unsupported feature holes.
        blockContext.RegisterSyntaxNodeAction(ctx =>
        {
            var invoc = (InvocationExpressionSyntax)ctx.Node;
            if (invoc.Expression is not MemberAccessExpressionSyntax memberAccess) return;

            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName != "Append" && methodName != "AppendLine") return;

            // Verify via semantic model that this is SqlBuilder.Append/AppendLine.
            var symbolInfo = ctx.SemanticModel.GetSymbolInfo(invoc);
            if (symbolInfo.Symbol is not IMethodSymbol method) return;
            if (method.ContainingType.ToDisplayString() != "SqlInterpol.SqlBuilder") return;

            // Resolve the dialect.
            string? dialect = null;
            switch (memberAccess.Expression)
            {
                case IdentifierNameSyntax receiverIdent:
                    variableDialects.TryGetValue(receiverIdent.Identifier.Text, out dialect);
                    break;

                case InvocationExpressionSyntax chainedFactory:
                    dialect = TryGetDialectFromFactoryCall(chainedFactory, ctx.SemanticModel);
                    break;
            }

            // If still unknown, check if we're inside a lambda passed to .Query<T>() on a
            // known-dialect builder: SqlBuilder.SqLite().Query<T>(p => <here>).
            // The outer builder's dialect constrains which SQL features are legal here.
            dialect ??= TryGetDialectFromLambdaContext(invoc, variableDialects, ctx.SemanticModel);

            if (dialect == null) return;
            if (!DialectFeatures.TryGetValue(dialect, out var supportedFeatures)) return;

            // Inspect every interpolated-string hole.
            var args = invoc.ArgumentList.Arguments;
            if (args.Count == 0) return;
            if (args[0].Expression is not InterpolatedStringExpressionSyntax interpolated) return;

            foreach (var content in interpolated.Contents)
            {
                // Literal text spans — scan for feature keywords written directly in the SQL
                // (e.g. "FOR SHARE", "ON CONFLICT"). The runtime parser tags these through
                // segment detection, so they will throw at runtime just like fragment holes.
                if (content is InterpolatedStringTextSyntax textSyntax)
                {
                    var rawText = textSyntax.TextToken.ValueText;
                    var stripped = StripSqlStringLiterals(rawText);
                    foreach (var (textFeatureKey, pattern) in TextFeaturePatterns)
                    {
                        if (pattern.IsMatch(stripped) && !supportedFeatures.Contains(textFeatureKey))
                        {
                            ctx.ReportDiagnostic(Diagnostic.Create(
                                Rule,
                                textSyntax.GetLocation(),
                                FeatureDisplayNames[textFeatureKey],
                                dialect));
                        }
                    }
                    continue;
                }

                // Interpolated holes — check for ISqlFeatureRequirement fragment objects.
                if (content is not InterpolationSyntax hole) continue;

                var featureInfo = TryGetRequiredFeature(hole.Expression, ctx.SemanticModel);
                if (featureInfo == null) continue;

                var (holeFeatureKey, displayName) = featureInfo.Value;
                if (!supportedFeatures.Contains(holeFeatureKey))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        hole.GetLocation(),
                        displayName,
                        dialect));
                }
            }
        }, SyntaxKind.InvocationExpression);
    }

    // Returns the dialect name (e.g. "PostgreSql") if the invocation is SqlBuilder.X().
    private static string? TryGetDialectFromFactoryCall(InvocationExpressionSyntax invoc, SemanticModel model)
    {
        var symbolInfo = model.GetSymbolInfo(invoc);
        if (symbolInfo.Symbol is not IMethodSymbol method) return null;
        if (method.ContainingType.ToDisplayString() != "SqlInterpol.SqlBuilder") return null;
        if (!method.IsStatic) return null;

        // Named factory methods: PostgreSql(), SqlServer(), MySql(), SqLite(), Oracle()
        if (DialectFeatures.ContainsKey(method.Name))
            return method.Name;

        // Generic Dialect<TDialect>() — map from dialect class name to factory key
        if (method.Name == "Dialect" && method.TypeArguments.Length == 1)
        {
            var dialectTypeName = method.TypeArguments[0].Name;
            return DialectClassToFactoryName(dialectTypeName);
        }

        return null;
    }

    // Maps dialect class names (e.g. "PostgreSqlSqlDialect") to factory method names ("PostgreSql").
    private static string? DialectClassToFactoryName(string className) => className switch
    {
        "PostgreSqlSqlDialect" => "PostgreSql",
        "SqlServerSqlDialect"  => "SqlServer",
        "MySqlSqlDialect"      => "MySql",
        "SqLiteSqlDialect"     => "SqLite",
        "OracleSqlDialect"     => "Oracle",
        _ => null
    };

    // Returns (featureKey, displayName) if the expression is a known ISqlFeatureRequirement type.
    private static (string FeatureKey, string DisplayName)? TryGetRequiredFeature(
        ExpressionSyntax expr, SemanticModel model)
    {
        var typeInfo = model.GetTypeInfo(expr);
        var type = typeInfo.Type;
        if (type == null) return null;

        // Only flag known SqlInterpol fragment types that implement ISqlFeatureRequirement.
        // We check by full type name to avoid false positives from user-defined types.
        const string FeatureRequirementInterface = "SqlInterpol.ISqlFeatureRequirement";
        var implementsInterface = false;
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString() == FeatureRequirementInterface)
            {
                implementsInterface = true;
                break;
            }
        }
        if (!implementsInterface) return null;

        switch (type.Name)
        {
            case "SqlReturningFragment":
                return ("Returning", FeatureDisplayNames["Returning"]);

            case "SqlLockFragment":
            {
                // Determine ForUpdate vs ForShare by inspecting the SqlLockMode constructor argument.
                var modeName = TryGetLockModeName(expr);
                return modeName switch
                {
                    "Update" => ("ForUpdate", FeatureDisplayNames["ForUpdate"]),
                    "Share"  => ("ForShare",  FeatureDisplayNames["ForShare"]),
                    // NoLock: matches ForShare in the runtime code, but NoLock is only used
                    // by SQL Server which already supports ForShare — don't flag.
                    _ => null
                };
            }

            default:
                // Unknown ISqlFeatureRequirement implementor — we cannot determine the feature
                // without running code, so skip to avoid false positives.
                return null;
        }
    }

    // Reads the first argument's member name from a SqlLockFragment construction expression.
    // Handles both `new SqlLockFragment(SqlLockMode.Update)` and `new(...) { Mode = ... }`.
    private static string? TryGetLockModeName(ExpressionSyntax expr)
    {
        // Standard construction: new SqlLockFragment(SqlLockMode.Update)
        if (expr is ObjectCreationExpressionSyntax { ArgumentList: { } argList } &&
            argList.Arguments.Count > 0)
        {
            return ExtractMemberName(argList.Arguments[0].Expression);
        }

        // Target-typed new: new(SqlLockMode.Update)
        if (expr is ImplicitObjectCreationExpressionSyntax { ArgumentList: { } implicitArgList } &&
            implicitArgList.Arguments.Count > 0)
        {
            return ExtractMemberName(implicitArgList.Arguments[0].Expression);
        }

        return null;
    }

    private static string? ExtractMemberName(ExpressionSyntax expr)
    {
        // SqlLockMode.Update → "Update"
        if (expr is MemberAccessExpressionSyntax member)
            return member.Name.Identifier.Text;

        return null;
    }

    // Walks up the syntax tree from an Append call to detect if it is inside a lambda
    // argument of .Query<T>() called on a known-dialect builder, e.g.:
    //   SqlBuilder.SqLite().Query<Product>(p => anyVar.Append($$"""..."""))
    // The outer builder's dialect is used when the Append receiver's dialect is unknown.
    private static string? TryGetDialectFromLambdaContext(
        InvocationExpressionSyntax appendInvoc,
        Dictionary<string, string> variableDialects,
        SemanticModel model)
    {
        SyntaxNode? current = appendInvoc.Parent;
        while (current != null)
        {
            // Don't cross declaration boundaries.
            if (current is BaseMethodDeclarationSyntax or
                TypeDeclarationSyntax or
                AccessorDeclarationSyntax)
                return null;

            if (current is LambdaExpressionSyntax &&
                current.Parent is ArgumentSyntax
                {
                    Parent: ArgumentListSyntax
                    {
                        Parent: InvocationExpressionSyntax queryInvoc
                    }
                })
            {
                if (queryInvoc.Expression is not MemberAccessExpressionSyntax queryMember)
                    break;

                // Confirm the outer call is a Query method whose receiver is a SqlBuilder.
                var receiverType = model.GetTypeInfo(queryMember.Expression).Type;
                if (receiverType?.ToDisplayString() != "SqlInterpol.SqlBuilder")
                    break;

                // Determine the dialect from the receiver of .Query<T>().
                switch (queryMember.Expression)
                {
                    case InvocationExpressionSyntax factoryCall:
                        return TryGetDialectFromFactoryCall(factoryCall, model);

                    case IdentifierNameSyntax queryReceiverIdent:
                        variableDialects.TryGetValue(queryReceiverIdent.Identifier.Text, out var d);
                        return d;
                }

                break;
            }

            current = current.Parent;
        }
        return null;
    }

    // Replaces content inside SQL string literals ('...') with spaces so that feature keyword
    // patterns are not matched against string data.
    private static string StripSqlStringLiterals(string text)
    {
        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\'')
            {
                sb.Append(' ');
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\'' && i + 1 < text.Length && text[i + 1] == '\'') { sb.Append("  "); i += 2; }
                    else if (text[i] == '\'') { sb.Append(' '); i++; break; }
                    else { sb.Append(' '); i++; }
                }
            }
            else
            {
                sb.Append(text[i++]);
            }
        }
        return sb.ToString();
    }
}
