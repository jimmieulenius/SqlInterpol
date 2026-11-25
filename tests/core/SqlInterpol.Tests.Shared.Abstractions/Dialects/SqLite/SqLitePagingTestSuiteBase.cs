using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLitePagingTestSuiteBase : IPagingTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "Products"."Id", "Products"."PROD_NAME"
            FROM "Products"
            ORDER BY "Products"."Id"
            LIMIT @p1 OFFSET @p2
            """
        ],
        expectedParameters: _expectedParameters
    )];
}