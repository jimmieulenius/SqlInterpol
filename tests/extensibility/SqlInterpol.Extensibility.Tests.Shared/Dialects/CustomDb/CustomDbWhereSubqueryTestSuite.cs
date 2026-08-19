using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbWhereSubqueryTestSuite : IWhereSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> WhereInSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                <<c>>.<<Name>>
            FROM <<Category>> AS <<c>>
            WHERE <<c>>.<<Id>> IN
            (
                SELECT 
                    <<p>>.<<CategoryId>>
                FROM <<dbo>>.<<Products>> AS <<p>>
                WHERE <<p>>.<<Price>> > 0
            )
            """
        ]
    )];
}