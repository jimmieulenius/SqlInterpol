using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleJoinSubqueryTestSuite : IJoinSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> JoinSubqueryData => [new SqlTestCase([
        """
        SELECT 
            "c"."Name", 
            "stats"."TotalPrice"
        FROM "Category" "c"
        LEFT JOIN
        (
            SELECT 
                "p"."CategoryId" AS "CategoryId",
                SUM("p"."Price") AS "TotalPrice"
            FROM "dbo"."Products" "p"
            GROUP BY "p"."CategoryId"
        ) "stats"
            ON "stats"."CategoryId" = "c"."Id"
        """
    ])];
}