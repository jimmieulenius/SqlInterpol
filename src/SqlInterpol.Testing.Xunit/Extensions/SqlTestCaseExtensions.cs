using Xunit;

namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// Provides extension methods for executing and asserting <see cref="SqlTestCase"/> objects.
/// </summary>
public static class SqlTestCaseExtensions
{
    /// <summary>
    /// Executes the query build action and appends the primitive results (or exceptions) to the TestCase state.
    /// </summary>
    /// <param name="testCase">The test case context.</param>
    /// <param name="action">The function that executes the query generation.</param>
    public static void Act(this SqlTestCase testCase, Func<SqlQueryResult> action)
    {
        if (testCase.ExpectedExceptionType != null)
        {
            var ex = Record.Exception(action);
            if (ex != null) testCase.ActualExceptions.Add(ex);
        }
        else
        {
            var result = action();
            testCase.ActualSql.Add(result.Sql);
            testCase.ActualParametersList.Add(result.Parameters.Values.ToArray());
        }
    }

    /// <summary>
    /// Executes a query build action that returns multiple queries and appends all results to the TestCase state.
    /// </summary>
    /// <param name="testCase">The test case context.</param>
    /// <param name="action">The function that executes the batch query generation.</param>
    public static void Act(this SqlTestCase testCase, Func<IEnumerable<SqlQueryResult>> action)
    {
        if (testCase.ExpectedExceptionType != null)
        {
            var ex = Record.Exception(action);
            if (ex != null) testCase.ActualExceptions.Add(ex);
        }
        else
        {
            var results = action();
            foreach (var result in results)
            {
                testCase.ActualSql.Add(result.Sql);
                testCase.ActualParametersList.Add(result.Parameters.Values.ToArray());
            }
        }
    }

    /// <summary>
    /// Automatically routes to the correct assertions based on the Expected state, validating all queries and parameters.
    /// </summary>
    /// <param name="testCase">The test case context.</param>
    public static void Assert(this SqlTestCase testCase)
    {
        if (testCase.ExpectedExceptionType != null)
        {
            testCase.Assert.Exception();
        }
        else
        {
            testCase.Assert.Sql();
            testCase.Assert.Parameters();
        }
    }
}