using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerJoinTestSuite : IJoinTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> JoinTwoEntitiesData => [new SqlTestCase([
        """
        SELECT
            [dbo].[Products].[Id],
            [OrderLine].[OrderId]
        FROM [dbo].[Products]
        JOIN [OrderLine]
            ON [dbo].[Products].[Id] = [OrderLine].[ProductItemNumber]
        """
    ])];
}