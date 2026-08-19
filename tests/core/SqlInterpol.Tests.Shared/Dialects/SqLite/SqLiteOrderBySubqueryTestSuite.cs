using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteOrderBySubqueryTestSuite : IOrderBySubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

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