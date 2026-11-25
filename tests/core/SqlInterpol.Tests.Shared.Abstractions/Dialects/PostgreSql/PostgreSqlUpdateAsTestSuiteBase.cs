using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public abstract partial class PostgreSqlUpdateAsTestSuiteBase : IUpdateAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

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