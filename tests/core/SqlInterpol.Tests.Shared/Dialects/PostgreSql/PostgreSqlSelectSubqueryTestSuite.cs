using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlSelectSubqueryTestSuite : ISelectSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> SelectSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = $1
                ) AS "CategoryName"
            FROM "dbo"."Products" AS "prod"
            WHERE "prod"."Price" > $2
            """,
            """
            SELECT 
                "second_prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = $1
                ) AS "CategoryName"
            FROM "dbo"."Products" AS "second_prod"
            WHERE "second_prod"."Price" > $2
            """
        ],
        expectedParameters: [100, 101]
    )];
}