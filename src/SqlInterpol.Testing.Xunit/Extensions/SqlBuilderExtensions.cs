using System.Reflection;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// Provides extension methods for testing and inspecting the internal state of a <see cref="SqlBuilder"/>.
/// </summary>
public static class SqlBuilderTestExtensions
{
    /// <summary>
    /// Asserts that the SQL Builder executed through the expected compilation pipeline 
    /// (AOT interceptors for AOT projects, JIT reflection for JIT projects).
    /// </summary>
    /// <param name="builder">The SQL builder instance being tested.</param>
    /// <exception cref="InvalidOperationException">Thrown when the query executes through the incorrect compilation pipeline.</exception>
    public static void AssertAotIntercepted(this SqlBuilder builder)
    {
        // Get the test assembly that invoked this extension method
        var testAssembly = Assembly.GetCallingAssembly();
        bool isAotProject = testAssembly.GetCustomAttribute<SqlInterpolAotEnabledAttribute>() != null;

        if (isAotProject)
        {
            if (!builder.LastBuildWasAotIntercepted)
            {
                throw new InvalidOperationException(
                    "AOT Assertion Failed: This query executed via the JIT reflection pipeline. " +
                    "Check your build warnings to see why the source generator rejected the query structure."
                );
            }
        }
        else
        {
            if (builder.LastBuildWasAotIntercepted)
            {
                throw new InvalidOperationException(
                    "JIT Assertion Failed: This query executed via AOT, but the JIT test project is running!"
                );
            }
        }
    }

    /// <summary>
    /// Grants test-time access to the internal Segment Stream (Token List) for custom assertions.
    /// Allows developers to verify structural rewriters without exposing internal state globally.
    /// </summary>
    /// <param name="builder">The SQL builder instance being tested.</param>
    /// <param name="inspector">A lambda function that receives the active segment stream for assertion.</param>
    public static void InspectSegments(this SqlBuilder builder, Action<IReadOnlyList<SqlSegment>> inspector)
    {
        // Because of [InternalsVisibleTo], this project can read builder.Segments
        inspector(builder.Segments);
    }

    /// <summary>
    /// Grants test-time access to the internal scoped variables dictionary for custom assertions.
    /// </summary>
    /// <param name="builder">The SQL builder instance being tested.</param>
    /// <param name="inspector">A lambda function that receives the active scoped variables dictionary for assertion.</param>
    public static void InspectScopedVariables(this SqlBuilder builder, Action<IReadOnlyDictionary<string, ISqlEntityBase>> inspector)
    {
        inspector(builder.ScopedVariables);
    }
}