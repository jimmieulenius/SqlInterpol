using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlUpdateAsTestSuite : IUpdateAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders" AS "ord"
            SET "order_status" = $1, "Total" = $2
            WHERE "ord"."Id" = 1
            """
        ],
        expectedParameters: ["Shipped", 99.99m]
    )];
}