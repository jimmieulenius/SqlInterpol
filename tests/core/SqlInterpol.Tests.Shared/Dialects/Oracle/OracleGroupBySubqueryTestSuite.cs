using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleGroupBySubqueryTestSuite : IGroupBySubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> GroupBySubqueryData => [new SqlTestCase([
        """
        SELECT CategoryId, COUNT(*)
        FROM (
            SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
        ) "stats"
        GROUP BY "stats"."CategoryId"
        """
    ])];
}