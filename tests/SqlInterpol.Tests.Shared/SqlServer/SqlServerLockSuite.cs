using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Tests.Shared;

public class SqlServerLockSuite : LockTestSuiteBase<SqlServerLockSuite>
{
    static SqlServerLockSuite()
    {
        object?[] expectedParams = [5];

        SelectWithForUpdateTheory = new SqlTestTheory(() =>
        {
            return [
                new SqlTestCase(
                    expectedSql: [
                        """
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products] WITH (UPDLOCK)
                        WHERE [dbo].[Products].[Id] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        })
        {
            AotCompatible = true
        };
        SelectWithForShareTheory = new SqlTestTheory(() =>
        {
            return [
                new SqlTestCase(
                    expectedSql: [
                        """
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products] WITH (ROWLOCK, HOLDLOCK)
                        WHERE [dbo].[Products].[Id] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        })
        {
            AotCompatible = true
        };
    }

    protected override SqlBuilder CreateBuilder() => SqlBuilder.SqlServer();
}