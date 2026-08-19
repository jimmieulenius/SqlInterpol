using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteWhereAsTestSuite : IWhereAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> WhereWithAliasedEntityData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "p"."Id" AS "ProductId"
            FROM "dbo"."Products" AS "p"
            WHERE "p"."Id" = @p1 AND "p"."CategoryId" = @p2
            """
        ],
        expectedParameters: [1, 5]
    )];
}