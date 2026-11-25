using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public abstract partial class SqlServerLockTestSuiteBase : ILockTestSuite
{
    private static object[] _expectedParameters = [5];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> SelectWithForUpdateData => 
    [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products] WITH (UPDLOCK)
                WHERE [dbo].[Products].[Id] = @p0
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];

    public static TheoryData<SqlTestCase> SelectWithForShareData => 
    [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products] WITH (ROWLOCK, HOLDLOCK)
                WHERE [dbo].[Products].[Id] = @p0
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}