using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdSetOperationTestSuiteBase : ISetOperationTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> QueryIntersectData => [new SqlTestCase(
        expectedSql: ["SELECT \"Products\".\"Id\" FROM \"Products\"\nINTERSECT\nSELECT \"Products\".\"Id\" FROM \"Products\" WHERE \"Products\".\"CategoryId\" = @p0"],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> QueryExceptData => [new SqlTestCase(
        expectedSql: ["SELECT \"Products\".\"Id\" FROM \"Products\"\nEXCEPT\nSELECT \"Products\".\"Id\" FROM \"Products\" WHERE \"Products\".\"CategoryId\" = @p0"],
        expectedParameters: [2]
    )];

    public static TheoryData<SqlTestCase> Select_UnionAllData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "Products"."Id", "Products"."PROD_NAME"
            FROM "Products"
            WHERE "Products"."CategoryId" = @p0
            UNION ALL
            SELECT "Products"."Id", "Products"."PROD_NAME"
            FROM "Products"
            WHERE "Products"."CategoryId" = @p1
            """
        ],
        expectedParameters: [1, 2]
    )];
}