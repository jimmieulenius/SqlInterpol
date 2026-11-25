using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleFromSubqueryTestSuiteBase : IFromSubqueryTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> From_SubqueryData => [new SqlTestCase([
        """
        SELECT
            "c"."Name",
            "stats"."TotalPrice"
        FROM
        (
            SELECT
                "p"."CategoryId" AS "CategoryId",
                SUM("p"."Price") AS "TotalPrice"
            FROM "dbo"."Products" "p"
            GROUP BY "p"."CategoryId"
        ) "stats"
        JOIN "Category" "c" ON "stats"."CategoryId" = "c"."Id"
        """
    ])];
}