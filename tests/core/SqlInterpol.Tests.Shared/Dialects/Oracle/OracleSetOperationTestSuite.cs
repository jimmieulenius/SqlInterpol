using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleSetOperationTestSuite : ISetOperationTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> QueryIntersectData => [new SqlTestCase(
        expectedSql: ["SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"\nINTERSECT\nSELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\" WHERE \"dbo\".\"Products\".\"CategoryId\" = :0"],
        expectedParameters: [1]
    )];

    // Oracle MUST use MINUS instead of EXCEPT!
    public static TheoryData<SqlTestCase> QueryExceptData => [new SqlTestCase(
        expectedSql: ["SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"\nMINUS\nSELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\" WHERE \"dbo\".\"Products\".\"CategoryId\" = :0"],
        expectedParameters: [2]
    )];

    public static TheoryData<SqlTestCase> Select_UnionAllData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."CategoryId" = :0
            UNION ALL
            SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."CategoryId" = :1
            """
        ],
        expectedParameters: [1, 2]
    )];
}