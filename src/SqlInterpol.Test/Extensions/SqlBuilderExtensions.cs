namespace SqlInterpol.Test;

public static class SqlBuilderExtensions
{
    /// <summary>
    /// Asserts that the SQL Builder successfully routed the query through the active pipeline 
    /// (AOT for the AOT test project, JIT for the JIT test project).
    /// </summary>
    public static void AssertAotIntercepted(this SqlBuilder builder)
    {
#if AOT_ENABLED
        if (!builder.LastBuildWasAotIntercepted)
        {
            throw new InvalidOperationException(
                "AOT Assertion Failed: This query executed via the JIT reflection pipeline. " +
                "Check your build warnings to see why the source generator rejected the query structure."
            );
        }
#else
        if (builder.LastBuildWasAotIntercepted)
        {
            throw new InvalidOperationException(
                "JIT Assertion Failed: This query somehow executed via AOT, but the JIT project is running!"
            );
        }
#endif
    }
}