using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public abstract partial class PostgreSqlPagingTestSuiteBase : IPagingTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            ORDER BY "dbo"."Products"."Id"
            LIMIT $1 OFFSET $2
            """
        ],
        expectedParameters: _expectedParameters
    )];
}