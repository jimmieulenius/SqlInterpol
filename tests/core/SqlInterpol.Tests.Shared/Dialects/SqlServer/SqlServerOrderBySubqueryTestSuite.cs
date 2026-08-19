using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerOrderBySubqueryTestSuite : IOrderBySubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> OrderBySubqueryData => [new SqlTestCase([
        """
        SELECT *
        FROM
        (
            SELECT
                CategoryId,
                MAX(Price) AS MaxPrice
            FROM Products
            GROUP BY CategoryId
        ) AS [stats]
        ORDER BY [stats].[MaxPrice] DESC
        """
    ])];
}