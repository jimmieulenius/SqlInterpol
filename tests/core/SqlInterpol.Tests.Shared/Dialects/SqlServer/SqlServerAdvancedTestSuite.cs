using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerAdvancedTestSuite : IAdvancedTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> DynamicQueryData =>
    [
        new SqlTestCase(
            [
                """
                SELECT [stats].[OrderId], [stats].[TotalAmount]
                FROM (
                    SELECT 
                        [o].[CustomerId],
                        [o].[Id] AS [OrderId], 
                        SUM([ol].[Price]) AS [TotalAmount]
                    FROM [dbo].[Orders] AS [o]
                    JOIN [OrderLine] AS [ol] ON [o].[Id] = [ol].[OrderId]
                    GROUP BY [o].[CustomerId], [o].[Id]
                ) AS [stats]
                WHERE [stats].[CustomerId] = @p0
                ORDER BY [stats].[TotalAmount] DESC
                OFFSET @p2 ROWS FETCH NEXT @p1 ROWS ONLY
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> AdvancedDynamicQueryData =>
    [
        new SqlTestCase(
            [
                """
                SELECT [stats].[OrderId], [stats].[ProductName], [stats].[TotalAmount]
                FROM (
                    SELECT 
                        [o].[Id] AS [OrderId], 
                        [p].[PROD_NAME] AS [ProductName],
                        [ol_agg].[TotalAmount] AS [TotalAmount]
                    FROM [dbo].[Orders] AS [o]
                    
                    JOIN (
                        SELECT 
                            [ol].[OrderId] AS [OrderId],
                            [ol].[ProductId] AS [ProductId],
                            SUM([ol].[Price]) AS [TotalAmount]
                        -- JOIN ol_agg ON ...
                        FROM [OrderLine] AS [ol]
                        GROUP BY [ol].[OrderId], [ol].[ProductId]
                    ) AS [ol_agg] ON [o].[Id] = [ol_agg].[OrderId]
                    
                    JOIN [dbo].[Products] AS [p] ON [ol_agg].[ProductId] = [p].[Id]
                    JOIN [Category] AS [cat] ON [p].[CategoryId] = [cat].[Id]
                ) AS [stats]
                WHERE [stats].[ProductName] = @p0
                ORDER BY [stats].[TotalAmount] DESC
                OFFSET @p2 ROWS FETCH NEXT @p1 ROWS ONLY
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> ComplexRawSqlData =>
    [
        new SqlTestCase(
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[Price] > @p0
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                HAVING COUNT(*) > 1
                ORDER BY [dbo].[Products].[PROD_NAME] DESC
                OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY
                """
            ]
        )
    ];
}