using Microsoft.CodeAnalysis;

namespace SqlInterpol.Generators;

/// <summary>
/// Central registry of all <see cref="DiagnosticDescriptor"/> instances emitted by
/// SqlInterpol's source generators. Keeping every descriptor here ensures that diagnostic
/// IDs, messages, and severities are defined in exactly one place.
/// </summary>
public static class SqlAotDiagnostics
{
    /// <summary>
    /// <c>SQLIG10</c> — emitted at a specific <c>Append</c> / <c>AppendLine</c> call-site
    /// when the interpolated string contains a dynamic construct (window function, set
    /// operation, upsert, etc.) that the AOT emitter cannot unroll at compile time.
    /// The call-site falls back to the JIT reflection pipeline at runtime.
    /// </summary>
    public static readonly DiagnosticDescriptor JitFallbackDiagnostic = new(
        id: "SQLIG10",
        title: "AOT Interception Skipped",
        messageFormat: "This Append call cannot be AOT-intercepted because it contains {0}. It will execute via the JIT pipeline.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Emitted when a SQL query uses dynamic features that force a fallback to the reflection-based JIT pipeline."
    );

    /// <summary>
    /// <c>SQLIG03</c> — emitted once per compilation when the
    /// <c>&lt;SqlInterpolDisableAot&gt;</c> MSBuild property is set to <c>true</c>.
    /// All <c>Append</c> calls in the project will use the JIT pipeline.
    /// </summary>
    public static readonly DiagnosticDescriptor AotDisabledDiagnostic = new(
        id: GeneratorConstants.DiagnosticAotDisabled,
        title: "SqlInterpol AOT Disabled",
        messageFormat: "SqlInterpol AOT generation has been disabled via the <SqlInterpolDisableAot> MSBuild property. Falling back to runtime JIT compilation.",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AOT interception is globally disabled for this project. Remove <SqlInterpolDisableAot> to re-enable compile-time SQL emission."
    );

    /// <summary>
    /// <c>SQLIG01</c> — emitted when a <c>[SqlQuery]</c> method's containing class is not
    /// <c>static</c>. The source generator cannot emit a valid extension-method wrapper for
    /// non-static containers.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidContainerDiagnostic = new(
        id: GeneratorConstants.DiagnosticInvalidContainer,
        title: "Invalid SqlQuery Container",
        messageFormat: "The class '{0}' must be static to contain a [SqlQuery] method.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Classes that host [SqlQuery] methods must be declared static so the generator can emit a valid public extension-method wrapper."
    );

    /// <summary>
    /// <c>SQLIG02</c> — emitted when a <c>[SqlQuery]</c> method already carries the
    /// <c>this</c> modifier. The generator automatically adds <c>this</c> to the emitted
    /// public wrapper; a duplicate modifier would cause a compile error.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidSignatureDiagnostic = new(
        id: GeneratorConstants.DiagnosticInvalidSignature,
        title: "Invalid SqlQuery Signature",
        messageFormat: "The method '{0}' cannot be an extension method. Remove the 'this' modifier from the first parameter. The source generator will automatically emit the 'this' modifier on the generated public wrapper.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [SqlQuery] source generator automatically makes the private implementation method a public extension method. Adding 'this' manually would result in a duplicate-modifier compile error."
    );

    /// <summary>
    /// <c>SQLIG11</c> — emitted when a <c>[SqlTest("X")]</c> attribute on a template method
    /// references a data property name <c>X</c> that is not declared as a <c>static abstract</c>
    /// member on the suite contract interface. Without a matching interface member the generator
    /// cannot emit a valid <c>[MemberData(nameof(X))]</c>, so the test method is silently skipped.
    /// </summary>
    public static readonly DiagnosticDescriptor MismatchedTestDataMemberDiagnostic = new(
        id: GeneratorConstants.DiagnosticMismatchedTestDataMember,
        title: "Mismatched SqlTest Data Property",
        messageFormat: "[SqlTest(\"{0}\")] on template method '{1}' in '{2}' references a property that does not exist on interface '{3}'. Add 'static abstract TheoryData<SqlTestCase> {0} {{ get; }}' to the interface, or correct the property name in [SqlTest].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Emitted when a [SqlTest] attribute string does not match any static abstract member declared on the suite contract interface, which would cause the test method to be silently dropped from the generated output."
    );
}