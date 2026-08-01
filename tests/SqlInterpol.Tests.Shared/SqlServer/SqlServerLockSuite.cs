using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.SqlServer;

public partial class SqlServerLockSuite : ILockTestSuite
{
    public SqlBuilder CreateBuilder() => SqlBuilder.SqlServer();

    public static TheoryData<SqlTestCase> SelectWithForUpdateData => 
        new TheoryData<SqlTestCase>
        {
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                    FROM [dbo].[Products] WITH (UPDLOCK)
                    WHERE [dbo].[Products].[Id] = @p0
                    """
                ],
                expectedParameters: [5]
            )
        };

    public static TheoryData<SqlTestCase> SelectWithForShareData => 
        new TheoryData<SqlTestCase>
        {
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                    FROM [dbo].[Products] WITH (ROWLOCK, HOLDLOCK)
                    WHERE [dbo].[Products].[Id] = @p0
                    """
                ],
                expectedParameters: [5]
            )
        };
}