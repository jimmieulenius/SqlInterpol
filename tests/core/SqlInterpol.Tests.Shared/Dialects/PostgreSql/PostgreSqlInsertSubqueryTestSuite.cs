using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlInsertSubqueryTestSuite : IInsertSubqueryTestSuite
{
    private static readonly object[] _expectedParameters = [100];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> InsertSelectData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders" 
            ("Id", "Total")
            SELECT "OrderLine"."OrderId", "OrderLine"."Quantity"
            FROM "OrderLine"
            WHERE "OrderLine"."OrderId" = $1
            """
        ],
        expectedParameters: _expectedParameters
    )];
}