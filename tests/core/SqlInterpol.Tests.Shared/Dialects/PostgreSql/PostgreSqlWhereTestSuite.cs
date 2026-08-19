using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlWhereTestSuite : IWhereTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> WhereSimpleParameterData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."Id" = $1
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
            WHERE "dbo"."Products"."CategoryId" IN ($1, $2, $3)
            """
        ],
        expectedParameters: [10, 20, 30]
    )];
}