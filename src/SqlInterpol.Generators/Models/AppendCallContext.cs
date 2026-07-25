using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

/// <summary>
/// Captures a single <c>Append</c> or <c>AppendLine</c> call-site as identified by
/// <see cref="SqlAotSyntaxWalker"/>, along with the metadata needed for
/// <c>[InterceptsLocation]</c> attribute generation.
/// </summary>
/// <param name="InvocationNode">The Roslyn syntax node for the invocation expression.</param>
/// <param name="MethodName">
/// Either <c>"Append"</c> or <c>"AppendLine"</c> (see <see cref="GeneratorConstants.MethodAppend"/>
/// and <see cref="GeneratorConstants.MethodAppendLine"/>).
/// </param>
/// <param name="HandlerTypeDisplayString">
/// The fully qualified display name of the interpolated string handler type
/// (e.g., <c>SqlInterpol.SqlQueryInterpolatedStringHandler</c>), used to emit the
/// correct interceptor parameter signature.
/// </param>
public record AppendCallContext(
    InvocationExpressionSyntax InvocationNode,
    string MethodName,
    string HandlerTypeDisplayString
);