namespace SqlInterpol.Generators;

/// <summary>
/// Holds all compile-time SQL state extracted by <see cref="SqlAotSyntaxWalker"/> for a single
/// containing method. Consumed by the emitter to generate <c>[InterceptsLocation]</c> interceptors.
/// </summary>
public class CompileTimeQueryContext
{
    /// <summary>
    /// Gets all entity variable declarations found in the method, keyed by C# variable name
    /// (e.g., <c>"ol"</c> for <c>db.Entity&lt;OrderLine&gt;(out var ol)</c>).
    /// </summary>
    public Dictionary<string, EntityDeclaration> Entities { get; } = new();

    /// <summary>
    /// Gets all <c>Append</c> / <c>AppendLine</c> call-sites found in the method that are
    /// candidates for AOT interception.
    /// </summary>
    public List<AppendCallContext> AppendCalls { get; } = new();

    /// <summary>
    /// Gets the set of variable names that were consumed by a <c>.Query(...)</c> call and
    /// therefore rendered as subqueries rather than direct table references.
    /// </summary>
    public HashSet<string> SubqueryEntities { get; } = new();

    /// <summary>
    /// Gets the set of variable names that hold the result of a <c>.Query(...)</c> call
    /// (either via <c>out var</c> parameter or direct assignment). When these appear as
    /// interpolation holes they require runtime rendering and force a JIT fallback.
    /// </summary>
    public HashSet<string> QueryFragmentVariables { get; } = new();
}