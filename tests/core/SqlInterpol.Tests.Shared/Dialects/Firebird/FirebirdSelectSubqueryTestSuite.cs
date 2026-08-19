using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdSelectSubqueryTestSuite : ISelectSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> SelectSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = @p0
                ) AS "CategoryName"
            FROM "dbo"."Products" AS "prod"
            WHERE "prod"."Price" > @p1
            """,
            """
            SELECT 
                "second_prod"."Id",
                (
                    SELECT
                        "Category"."Name"
                    FROM "Category"
                    WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = @p0
                ) AS "CategoryName"
            FROM "dbo"."Products" AS "second_prod"
            WHERE "second_prod"."Price" > @p1
            """
        ],
        expectedParameters: [100, 101]
    )];
}