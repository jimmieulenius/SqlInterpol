using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleWhereAsTestSuite : IWhereAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> WhereWithAliasedEntityData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "p"."Id" AS "ProductId"
            FROM "dbo"."Products" "p"
            WHERE "p"."Id" = :0 AND "p"."CategoryId" = :1
            """
        ],
        expectedParameters: [1, 5]
    )];
}