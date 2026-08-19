using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbHavingTestSuite : IHavingTestSuite
{
    private static readonly object[] _expectedParameters = [5];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> SelectGroupByAndHavingData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                <<dbo>>.<<Products>>.<<CategoryId>>,
                COUNT(<<dbo>>.<<Products>>.<<Id>>) AS <<ProductCount>>
            FROM <<dbo>>.<<Products>>
            GROUP BY <<dbo>>.<<Products>>.<<CategoryId>>
            HAVING COUNT(<<dbo>>.<<Products>>.<<Id>>) > !!100
            """
        ],
        expectedParameters: _expectedParameters
    )];
}