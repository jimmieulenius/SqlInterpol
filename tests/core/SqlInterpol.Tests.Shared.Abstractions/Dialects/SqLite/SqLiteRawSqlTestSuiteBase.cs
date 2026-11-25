using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteRawSqlTestSuiteBase : IRawSqlTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> ComplexRawSqlData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "Products"."Id", "Products"."PROD_NAME"
            FROM "Products"
            WHERE "Products"."Price" > @p1
              AND p.Status = 'ACTIVE' /* Raw SQL condition */
            GROUP BY "Products"."Id", "Products"."PROD_NAME"
            HAVING COUNT(*) > 1
            ORDER BY "Products"."PROD_NAME" DESC
            LIMIT 10 OFFSET 5
            """
        ],
        expectedParameters: [50.00m]
    )];

    public static TheoryData<SqlTestCase> WindowFunctionData => [new SqlTestCase(
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