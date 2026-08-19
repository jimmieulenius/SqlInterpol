using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleSelectSubqueryTestSuite : ISelectSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> SelectSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = :0
                ) AS "CategoryName"
            FROM "dbo"."Products" "prod"
            WHERE "prod"."Price" > :1
            """,
            """
            SELECT 
                "second_prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = :0
                ) AS "CategoryName"
            FROM "dbo"."Products" "second_prod"
            WHERE "second_prod"."Price" > :1
            """
        ],
        expectedParameters: [100, 101]
    )];
}