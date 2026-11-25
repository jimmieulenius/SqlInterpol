using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public abstract partial class PostgreSqlOrderBySubqueryTestSuiteBase : IOrderBySubqueryTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

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
        ) AS "stats"
        ORDER BY "stats"."MaxPrice" DESC
        """
    ])];
}