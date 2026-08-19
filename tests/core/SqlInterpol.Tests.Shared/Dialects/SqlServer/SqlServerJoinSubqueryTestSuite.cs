using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerJoinSubqueryTestSuite : IJoinSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> JoinSubqueryData => [new SqlTestCase([
        """
        SELECT 
            [c].[Name], 
            [stats].[TotalPrice]
        FROM [Category] AS [c]
        LEFT JOIN
        (
            SELECT 
                [p].[CategoryId] AS [CategoryId],
                SUM([p].[Price]) AS [TotalPrice]
            FROM [dbo].[Products] AS [p]
            GROUP BY [p].[CategoryId]
        ) AS [stats]
            ON [stats].[CategoryId] = [c].[Id]
        """
    ])];
}