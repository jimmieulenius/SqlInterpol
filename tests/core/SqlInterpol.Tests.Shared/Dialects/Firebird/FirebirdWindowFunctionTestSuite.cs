using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdWindowFunctionTestSuite : IWindowFunctionTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> WindowFunctionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "dbo"."Products"."PROD_NAME",
                SUM("dbo"."Products"."Price") OVER (
                    PARTITION BY "dbo"."Products"."CategoryId"
                    ORDER BY "dbo"."Products"."Id" DESC
                ) AS CategoryTotal
            FROM "dbo"."Products"
            """
        ]
    )];

    public static TheoryData<SqlTestCase> RawWindowFunctionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "dbo"."Products"."PROD_NAME",
                "dbo"."Products"."Price",
                AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") AS "AvgCategoryPrice"
            FROM "dbo"."Products"
            """
        ]
    )];
}