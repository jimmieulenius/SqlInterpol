using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdWindowFunctionTestSuiteBase : IWindowFunctionTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> WindowFunctionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "Products"."PROD_NAME",
                SUM("Products"."Price") OVER (
                    PARTITION BY "Products"."CategoryId"
                    ORDER BY "Products"."Id" DESC
                ) AS CategoryTotal
            FROM "Products"
            """
        ]
    )];

    public static TheoryData<SqlTestCase> RawWindowFunctionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "Products"."PROD_NAME",
                "Products"."Price",
                AVG("Products"."Price") OVER (PARTITION BY "Products"."CategoryId") AS "AvgCategoryPrice"
            FROM "Products"
            """
        ]
    )];
}