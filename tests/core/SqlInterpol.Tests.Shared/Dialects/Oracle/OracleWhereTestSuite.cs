using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleWhereTestSuite : IWhereTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> WhereSimpleParameterData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."Id" = :0
            """
        ],
        expectedParameters: [42]
    )];

    public static TheoryData<SqlTestCase> WhereInCollectionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."CategoryId" IN (:0, :1, :2)
            """
        ],
        expectedParameters: [10, 20, 30]
    )];
}