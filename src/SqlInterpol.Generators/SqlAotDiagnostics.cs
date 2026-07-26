using Microsoft.CodeAnalysis;

namespace SqlInterpol.Generators;

public static class SqlAotDiagnostics
{
    public static readonly DiagnosticDescriptor JitFallbackDiagnostic = new(
        id: "SQLIG10",
        title: "AOT Interception Skipped",
        messageFormat: "This Append call cannot be AOT-intercepted because it contains {0}. It will execute via the JIT pipeline.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Emitted when a SQL query uses dynamic features that force a fallback to the reflection-based JIT pipeline."
    );
}