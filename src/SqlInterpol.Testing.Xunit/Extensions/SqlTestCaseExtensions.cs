using Xunit;

namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// Provides extension methods for executing and asserting <see cref="SqlTestCase"/> objects.
/// </summary>
public static class SqlTestCaseExtensions
{
    /// <summary>
    /// Executes a query build action that returns multiple queries and appends all results to the TestCase state.
    /// </summary>
    /// <param name="testCase">The test case context.</param>
    /// <param name="action">The function that executes the batch query generation.</param>
    public static void Act(this SqlTestCase testCase, Func<IEnumerable<SqlQueryResult>> action)
    {
        try
        {
            var results = action().ToArray();
            foreach (var result in results)
            {
                testCase.Act(() => result);
            }
        }
        catch (Exception ex)
        {
            testCase.ActualExceptions.Add(ex);
        }
    }

    /// <summary>
    /// Automatically routes to the correct assertions based on the Expected state, validating exceptions,
    /// generated SQL, parameters, and E2E database results.
    /// </summary>
    /// <param name="testCase">The test case context.</param>
    public static void Assert(this SqlTestCase testCase)
    {
        if (testCase.ExpectedExceptionType != null)
        {
            testCase.Assert.Exception();
            return;
        }

        if (testCase.ActualExceptions.Count == 1)
        {
            // Re-throw preserving original stack trace so xUnit displays the exact DB/builder failure
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(testCase.ActualExceptions[0]).Throw();
        }
        else if (testCase.ActualExceptions.Count > 1)
        {
            throw new AggregateException(testCase.ActualExceptions);
        }

        testCase.Assert.Sql();
        testCase.Assert.Parameters();
        testCase.Assert.Data();
    }
}