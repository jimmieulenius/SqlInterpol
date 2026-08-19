using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlOrderBySubqueryTestSuite : IOrderBySubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

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
        ) AS `stats`
        ORDER BY `stats`.`MaxPrice` DESC
        """
    ])];
}