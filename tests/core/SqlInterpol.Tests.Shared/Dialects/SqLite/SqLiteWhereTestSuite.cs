using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteWhereTestSuite : IWhereTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> WhereSimpleParameterData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."Id" = @p1
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
            WHERE "dbo"."Products"."CategoryId" IN (@p1, @p2, @p3)
            """
        ],
        expectedParameters: [10, 20, 30]
    )];
}