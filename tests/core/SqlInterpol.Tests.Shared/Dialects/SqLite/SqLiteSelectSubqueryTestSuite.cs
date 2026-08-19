using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteSelectSubqueryTestSuite : ISelectSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> SelectSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = @p1
                ) AS "CategoryName"
            FROM "dbo"."Products" AS "prod"
            WHERE "prod"."Price" > @p2
            """,
            """
            SELECT 
                "second_prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = @p1
                ) AS "CategoryName"
            FROM "dbo"."Products" AS "second_prod"
            WHERE "second_prod"."Price" > @p2
            """
        ],
        expectedParameters: [100, 101]
    )];
}