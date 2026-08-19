using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLitePagingTestSuite : IPagingTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            ORDER BY "dbo"."Products"."Id"
            LIMIT @p1 OFFSET @p2
            """
        ],
        expectedParameters: _expectedParameters
    )];
}