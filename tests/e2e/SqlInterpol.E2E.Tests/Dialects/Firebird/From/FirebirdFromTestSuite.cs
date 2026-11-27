using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;
using SqlInterpol.Tests.Dialects.Firebird;
using Xunit;

namespace SqlInterpol.E2E.Tests.Dialects.Firebird;

[Trait("Category", "Firebird")]
[Trait("Category", "E2E")]
[Collection("Firebird E2E")]
public partial class FirebirdFromTestSuite : FirebirdFromTestSuiteBase
{
    private readonly FirebirdFixture _fixture;

    public FirebirdFromTestSuite(FirebirdFixture fixture)
    {
        _fixture = fixture;
        SqlTestCase.CurrentConnection.Value = _fixture.Connection;
    }

    public override SqlBuilder CreateBuilder(SqlInterpolOptions? options = null)
        => _fixture.CreateBuilder(options);

    protected override void ConfigureE2E(SqlTestCase testCase, string testName)
    {
        var (seed, expectedData) = testName switch
        {
            nameof(From_SingleEntity) => (
                SqlTestDbSeed.Path(testName),
                new SqlTestDbData
                {
                    Data = [
                        new OrderLine(OrderId: 1, ProductItemNumber: 100, Quantity: 5, Price: 9.99m, ProductId: 200),
                        new OrderLine(OrderId: 2, ProductItemNumber: 101, Quantity: 10, Price: 14.99m, ProductId: 201),
                        new OrderLine(OrderId: 3, ProductItemNumber: 102, Quantity: 1, Price: 99.99m, ProductId: 202)
                    ]
                }
            ),

            nameof(From_Entity_WithSqlTableNameOnly) => (
                SqlTestDbSeed.Path(testName),
                new SqlTestDbData
                {
                    Data = [new TableOnlyModel(Id: 1)]
                }
            ),

            nameof(From_Entity_WithSqlTableNameAndSchema) => (
                SqlTestDbSeed.Path("From_Entity_WithSqlTableNameOnly.sql"),
                new SqlTestDbData
                {
                    Data = [new TableAndSchemaModel(Id: 1)]
                }
            ),

            _ => (null, null)
        };

        _fixture.PrepareTest(suiteFolder: "From", seed);
        testCase.ExpectedData = expectedData;
    }
}