using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// A metadata wrapper around xUnit TheoryData for executing data-driven SQL test cases.
/// </summary>
public class SqlTestTheory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTestTheory"/> class.
    /// </summary>
    public SqlTestTheory()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTestTheory"/> class using the specified data provider function.
    /// </summary>
    /// <param name="dataProvider">A function that yields the <see cref="TheoryData{SqlTestCase}"/> to be used in the test execution matrix.</param>
    [SetsRequiredMembers]
    public SqlTestTheory(Func<TheoryData<SqlTestCase>> dataProvider)
    {
        Data = dataProvider();
    }

    /// <summary>
    /// Gets or initializes the collection of test cases supplied to the xUnit theory.
    /// </summary>
    public required TheoryData<SqlTestCase> Data { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether the queries in this theory are expected to be intercepted by the AOT source generator.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool AotCompatible { get; init; } = true;
}