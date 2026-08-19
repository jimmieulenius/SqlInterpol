using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerRawSqlTestSuite : IRawSqlTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> ComplexRawSqlData => [new SqlTestCase(
        expectedSql: [
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
        ],
        expectedParameters: [50.00m]
    )];

    public static TheoryData<SqlTestCase> WindowFunctionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                [dbo].[Products].[PROD_NAME],
                [dbo].[Products].[Price],
                AVG([dbo].[Products].[Price]) OVER (PARTITION BY [dbo].[Products].[CategoryId]) AS [AvgCategoryPrice]
            FROM [dbo].[Products]
            """
        ]
    )];
}