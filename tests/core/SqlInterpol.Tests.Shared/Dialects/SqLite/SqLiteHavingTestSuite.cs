using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteHavingTestSuite : IHavingTestSuite
{
    private static readonly object[] _expectedParameters = [5];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> SelectGroupByAndHavingData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "dbo"."Products"."CategoryId",
                COUNT("dbo"."Products"."Id") AS "ProductCount"
            FROM "dbo"."Products"
            GROUP BY "dbo"."Products"."CategoryId"
            HAVING COUNT("dbo"."Products"."Id") > @p1
            """
        ],
        expectedParameters: _expectedParameters
    )];
}