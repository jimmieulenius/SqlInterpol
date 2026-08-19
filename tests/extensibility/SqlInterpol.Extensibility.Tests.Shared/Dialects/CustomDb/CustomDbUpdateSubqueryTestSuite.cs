using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbUpdateSubqueryTestSuite : IUpdateSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> UpdateSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE (SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId) AS <<stats>>
            SET <<max_price>> = !!100
            WHERE <<stats>>.<<CategoryId>> = 5
            """
        ],
        expectedParameters: [99.99m]
    )];

    public static TheoryData<SqlTestCase> UpdateTypeSafeSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE (SELECT <<p>>.<<CategoryId>> AS <<CategoryId>>, MAX(<<p>>.<<Price>>) AS <<MaxPrice>> FROM <<dbo>>.<<Products>> AS <<p>> GROUP BY <<p>>.<<CategoryId>>) AS <<stats>>
            SET <<max_price>> = !!100
            WHERE <<stats>>.<<CategoryId>> = 5
            """
        ],
        expectedParameters: [99.99m]
    )];
}