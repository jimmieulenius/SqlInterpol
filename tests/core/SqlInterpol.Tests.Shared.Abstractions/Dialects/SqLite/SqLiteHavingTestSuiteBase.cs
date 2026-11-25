using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteHavingTestSuiteBase : IHavingTestSuite
{
    private static readonly object[] _expectedParameters = [5];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> SelectGroupByAndHavingData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "Products"."CategoryId",
                COUNT("Products"."Id") AS "ProductCount"
            FROM "Products"
            GROUP BY "Products"."CategoryId"
            HAVING COUNT("Products"."Id") > @p1
            """
        ],
        expectedParameters: _expectedParameters
    )];
}