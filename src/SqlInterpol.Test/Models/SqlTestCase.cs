using SqlInterpol.Configuration;
using Xunit.Abstractions;

namespace SqlInterpol.Test.Models;

public sealed class SqlTestCase : SqlTestCaseBase
{
    public SqlTestCaseAssert Assert => new(this);
    
    public List<string> ActualSql { get; } = [];
    public List<object?[]> ActualParametersList { get; } = [];
    public List<Exception> ActualExceptions { get; } = [];
    
    public Type? ActualExceptionType => ActualExceptions.FirstOrDefault()?.GetType();

    public string[]? ExpectedSql { get; private set; }
    public object?[] ExpectedParameters { get; private set; } = [];
    public Type? ExpectedExceptionType { get; private set; }
    public string? ExpectedExceptionMessage { get; private set; }

    public string? Id { get; private set; }

    // Required for xUnit deserialization
    public SqlTestCase() { }

    /// <summary>
    /// Creates a test case that expects a successful SQL string generation.
    /// </summary>
    public SqlTestCase(SqlDialectKind dialect, string[] expectedSql, object?[]? expectedParameters = null, string? id = null) : base(dialect)
    {
        ExpectedSql = expectedSql ?? throw new ArgumentNullException(nameof(expectedSql));
        if (expectedSql.Length == 0) throw new ArgumentException("ExpectedSql cannot be empty.", nameof(expectedSql));
        
        ExpectedParameters = expectedParameters ?? [];
        Id = id;

    }

    /// <summary>
    /// Creates a test case that expects an exception to be thrown for an unsupported dialect or invalid syntax.
    /// </summary>
    public SqlTestCase(SqlDialectKind dialect, Type expectedExceptionType, string? expectedExceptionMessage = null, string? id = null) : base(dialect)
    {
        ExpectedExceptionType = expectedExceptionType ?? throw new ArgumentNullException(nameof(expectedExceptionType));
        ExpectedExceptionMessage = expectedExceptionMessage;
        Id = id;
    }

    public override void Serialize(IXunitSerializationInfo info)
    {
        base.Serialize(info);
        info.AddValue(nameof(ExpectedSql), ExpectedSql);
        info.AddValue(nameof(ExpectedExceptionType), ExpectedExceptionType?.AssemblyQualifiedName);
        info.AddValue(nameof(ExpectedExceptionMessage), ExpectedExceptionMessage);
        info.AddValue(nameof(ExpectedParameters), ExpectedParameters);
        info.AddValue(nameof(Id), Id);
    }

    public override void Deserialize(IXunitSerializationInfo info)
    {
        base.Deserialize(info);
        ExpectedSql = info.GetValue<string[]>(nameof(ExpectedSql));
        ExpectedParameters = info.GetValue<object?[]>(nameof(ExpectedParameters)) ?? [];

        var expectedExceptionTypeName = info.GetValue<string>(nameof(ExpectedExceptionType));
        if (!string.IsNullOrEmpty(expectedExceptionTypeName))
        {
            ExpectedExceptionType = Type.GetType(expectedExceptionTypeName);
        }

        ExpectedExceptionMessage = info.GetValue<string>(nameof(ExpectedExceptionMessage));
        Id = info.GetValue<string>(nameof(Id));
    }

    public override string ToString()
    {
        var name = Dialect.ToString();

        // If an ID was provided, append it! (e.g., "CustomDb_1")
        if (!string.IsNullOrWhiteSpace(Id))
        {
            name += $"_{Id}";
        }

        return name;
    }
}

public sealed class SqlTestCaseAssert(SqlTestCase testCase)
{
    private readonly SqlTestCase _testCase = testCase;

    public void Sql()
    {
        Xunit.Assert.NotEmpty(_testCase.ActualSql);
        Xunit.Assert.NotNull(_testCase.ExpectedSql);
        
        // Ensure the test generated the exact number of queries expected
        Xunit.Assert.Equal(_testCase.ExpectedSql!.Length, _testCase.ActualSql.Count);

        const string WindowsLineEnding = "\r\n";
        const string UnixLineEnding = "\n";

        for (int i = 0; i < _testCase.ExpectedSql.Length; i++)
        {
            var expected = _testCase.ExpectedSql[i].Replace(WindowsLineEnding, UnixLineEnding);
            var actual = _testCase.ActualSql[i].Replace(WindowsLineEnding, UnixLineEnding);

            Xunit.Assert.Equal(expected, actual);
        }
    }

    public void Parameters()
    {
        if (_testCase.ExpectedParameters == null || _testCase.ExpectedParameters.Length == 0)
            return; // No parameters to verify

        Xunit.Assert.NotEmpty(_testCase.ActualParametersList);
        
        // Validates parameters against the first query in the sequence
        var actualParams = _testCase.ActualParametersList[0];

        Xunit.Assert.Equal(_testCase.ExpectedParameters.Length, actualParams.Length);

        for (int i = 0; i < _testCase.ExpectedParameters.Length; i++)
        {
            Xunit.Assert.Equal(_testCase.ExpectedParameters[i], actualParams[i]);
        }
    }

    public void Exception()
    {
        Xunit.Assert.NotNull(_testCase.ExpectedExceptionType);
        Xunit.Assert.NotEmpty(_testCase.ActualExceptions);
        
        var actualException = _testCase.ActualExceptions[0];
        
        Xunit.Assert.IsType(_testCase.ExpectedExceptionType!, actualException);

        if (!string.IsNullOrEmpty(_testCase.ExpectedExceptionMessage))
        {
            Xunit.Assert.Equal(_testCase.ExpectedExceptionMessage, actualException.Message);
        }
    }
}

public static class SqlTestCaseExtensions
{
    /// <summary>
    /// Executes the query build action and appends the primitive results (or exceptions) to the TestCase state.
    /// </summary>
    public static void Action(this SqlTestCase testCase, Func<SqlQueryResult> action)
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
    public static void Action(this SqlTestCase testCase, Func<IEnumerable<SqlQueryResult>> action)
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
    /// Automatically routes to the correct assertions based on the Expected state, validating all queries.
    /// </summary>
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