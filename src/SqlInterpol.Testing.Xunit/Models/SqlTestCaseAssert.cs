using Xunit;

namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// Provides contextual assertions for a specific <see cref="SqlTestCase"/> execution.
/// </summary>
public sealed class SqlTestCaseAssert
{
    private readonly SqlTestCase _testCase;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTestCaseAssert"/> class.
    /// </summary>
    /// <param name="testCase">The test case to assert against.</param>
    public SqlTestCaseAssert(SqlTestCase testCase)
    {
        _testCase = testCase;
    }

    /// <summary>
    /// Verifies that the actual generated SQL matches the expected SQL across all execution batches.
    /// </summary>
    public void Sql()
    {
        Assert.NotEmpty(_testCase.ActualSql);
        Assert.NotNull(_testCase.ExpectedSql);
        
        // Ensure the test generated the exact number of queries expected
        Assert.Equal(_testCase.ExpectedSql!.Length, _testCase.ActualSql.Count);

        for (int i = 0; i < _testCase.ExpectedSql.Length; i++)
        {
            // Defer to the standardized SqlAssert string matcher
            SqlAssert.MatchesSql(_testCase.ExpectedSql[i], _testCase.ActualSql[i]);
        }
    }

    /// <summary>
    /// Verifies that the parameters bound to the first generated query match the expected parameters.
    /// </summary>
    public void Parameters()
    {
        if (_testCase.ExpectedParameters == null || _testCase.ExpectedParameters.Length == 0)
            return; // No parameters to verify

        Assert.NotEmpty(_testCase.ActualParametersList);
        
        // Validates parameters against the first query in the sequence
        var actualParams = _testCase.ActualParametersList[0];
        
        // Defer to the standardized SqlAssert parameter matcher
        SqlAssert.MatchesParameters(_testCase.ExpectedParameters, actualParams);
    }

    /// <summary>
    /// Verifies that the expected exception was thrown during query generation.
    /// </summary>
    public void Exception()
    {
        Assert.NotNull(_testCase.ExpectedExceptionType);
        Assert.NotEmpty(_testCase.ActualExceptions);
        
        var actualException = _testCase.ActualExceptions[0];
        
        Assert.IsType(_testCase.ExpectedExceptionType!, actualException);

        if (!string.IsNullOrEmpty(_testCase.ExpectedExceptionMessage))
        {
            Assert.Equal(_testCase.ExpectedExceptionMessage, actualException.Message);
        }
    }
}