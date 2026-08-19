using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerJoinAsTestSuite : IJoinAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> JoinWithLiteralAliasesData => [new SqlTestCase([
        """
        SELECT
            [p].[Id],
            [ol].[OrderId]
        FROM [dbo].[Products] AS [p]
        JOIN [OrderLine] AS [ol]
            ON [p].[Id] = [ol].[ProductItemNumber]
        """
    ])];

    public static TheoryData<SqlTestCase> JoinWithExplicitApiAliasesData => [new SqlTestCase([
        """
        SELECT
            [prod].[Id],
            [OrderLine].[OrderId]
        FROM dbo.Products AS [prod]
        JOIN order_lines AS [OrderLine]
            ON [prod].[Id] = [OrderLine].[ProductItemNumber]
        """
    ])];

    public static TheoryData<SqlTestCase> SelfJoinData => [new SqlTestCase([
        """
        SELECT
            [original].[Id],
            [related].[Id]
        FROM [dbo].[Products] AS [original]
        JOIN [dbo].[Products] AS [related]
            ON [original].[CategoryId] = [related].[CategoryId]
        """
    ])];

    public static TheoryData<SqlTestCase> JoinWithConfigOverrideData => [new SqlTestCase([
        """
        SELECT
            [history].[Archive_Products].[Id],
            [OrderLine].[OrderId]
        FROM [history].[Archive_Products]
        JOIN [OrderLine]
            ON [history].[Archive_Products].[Id] = [OrderLine].[ProductItemNumber]
        """
    ])];
}