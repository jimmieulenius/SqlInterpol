using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteGroupBySubqueryTestSuite : IGroupBySubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> GroupBySubqueryData => [new SqlTestCase([
        """
        SELECT CategoryId, COUNT(*)
        FROM (
            SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
        ) AS "stats"
        GROUP BY "stats"."CategoryId"
        """
    ])];
}