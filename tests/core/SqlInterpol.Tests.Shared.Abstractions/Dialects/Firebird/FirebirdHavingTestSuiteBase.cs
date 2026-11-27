using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdHavingTestSuiteBase : IHavingTestSuite
{
    private static readonly object[] _expectedParameters = [5];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> SelectGroupByAndHavingData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "Products"."CategoryId",
                COUNT("Products"."Id") AS "ProductCount"
            FROM "Products"
            GROUP BY "Products"."CategoryId"
            HAVING COUNT("Products"."Id") > @p0
            """
        ],
        expectedParameters: _expectedParameters
    )];
}