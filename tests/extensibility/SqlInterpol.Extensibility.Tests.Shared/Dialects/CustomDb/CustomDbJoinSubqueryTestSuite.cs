using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbJoinSubqueryTestSuite : IJoinSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> JoinSubqueryData => [new SqlTestCase([
        """
        SELECT 
            <<c>>.<<Name>>, 
            <<stats>>.<<TotalPrice>>
        FROM <<Category>> AS <<c>>
        LEFT JOIN
        (
            SELECT 
                <<p>>.<<CategoryId>> AS <<CategoryId>>,
                SUM(<<p>>.<<Price>>) AS <<TotalPrice>>
            FROM <<dbo>>.<<Products>> AS <<p>>
            GROUP BY <<p>>.<<CategoryId>>
        ) AS <<stats>>
            ON <<stats>>.<<CategoryId>> = <<c>>.<<Id>>
        """
    ])];
}