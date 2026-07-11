using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

public record AppendCallContext(
    InvocationExpressionSyntax InvocationNode,
    string MethodName,
    string HandlerTypeDisplayString // Tracks the exact fully qualified type name
);