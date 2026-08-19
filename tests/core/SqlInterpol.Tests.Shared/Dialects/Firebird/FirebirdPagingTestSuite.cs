using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdPagingTestSuite : IPagingTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            ORDER BY "dbo"."Products"."Id"
            FIRST @p0 SKIP @p1
            """
        ],
        expectedParameters: _expectedParameters
    )];
}