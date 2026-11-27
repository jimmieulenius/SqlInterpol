using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdPagingTestSuiteBase : IPagingTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "Products"."Id", "Products"."PROD_NAME"
            FROM "Products"
            ORDER BY "Products"."Id"
            FIRST @p0 SKIP @p1
            """
        ],
        expectedParameters: _expectedParameters
    )];
}