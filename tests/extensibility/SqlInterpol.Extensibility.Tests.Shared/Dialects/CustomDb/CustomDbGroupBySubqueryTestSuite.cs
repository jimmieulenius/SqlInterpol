using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbGroupBySubqueryTestSuite : IGroupBySubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> GroupBySubqueryData => [new SqlTestCase([
        """
        SELECT CategoryId, COUNT(*)
        FROM (
            SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
        ) AS <<stats>>
        GROUP BY <<stats>>.<<CategoryId>>
        """
    ])];
}