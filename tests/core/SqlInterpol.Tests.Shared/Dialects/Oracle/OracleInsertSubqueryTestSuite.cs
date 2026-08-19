using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleInsertSubqueryTestSuite : IInsertSubqueryTestSuite
{
    private static readonly object[] _expectedParameters = [100];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> InsertSelectData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders" 
            ("Id", "Total")
            SELECT "OrderLine"."OrderId", "OrderLine"."Quantity"
            FROM "OrderLine"
            WHERE "OrderLine"."OrderId" = :0
            """
        ],
        expectedParameters: _expectedParameters
    )];
}