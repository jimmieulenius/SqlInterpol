using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdGroupBySubqueryTestSuiteBase : IGroupBySubqueryTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

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