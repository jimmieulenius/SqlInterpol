using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlWhereAsTestSuite : IWhereAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> WhereWithAliasedEntityData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "p"."Id" AS "ProductId"
            FROM "dbo"."Products" AS "p"
            WHERE "p"."Id" = $1 AND "p"."CategoryId" = $2
            """
        ],
        expectedParameters: [1, 5]
    )];
}