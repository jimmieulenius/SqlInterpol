using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IStoredProcedureTranspilationTestSuite))]
public abstract partial class StoredProcedureTranspilationTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IStoredProcedureTranspilationTestSuite.CallData))]
    public void StoredProcedure_Call(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            return [db.Append($"CALL process_order({1}, {"Active"})").Build()];
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}
