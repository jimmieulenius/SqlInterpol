using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleOrderBySubqueryTestSuiteBase : IOrderBySubqueryTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

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
        ) "stats"
        ORDER BY "stats"."MaxPrice" DESC
        """
    ])];
}