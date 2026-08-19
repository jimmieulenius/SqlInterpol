using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerPagingTestSuite : IPagingTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
            FROM [dbo].[Products]
            ORDER BY [dbo].[Products].[Id]
            OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
            """
        ],
        expectedParameters: _expectedParameters
    )];
}