using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbPagingTestSuite : IPagingTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
            FROM <<dbo>>.<<Products>>
            ORDER BY <<dbo>>.<<Products>>.<<Id>>
            LIMIT !!100 OFFSET !!101
            """
        ],
        expectedParameters: _expectedParameters
    )];
}