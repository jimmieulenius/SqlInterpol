using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbWhereTestSuite : IWhereTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> WhereSimpleParameterData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>
            WHERE <<dbo>>.<<Products>>.<<Id>> = !!100
            """
        ],
        expectedParameters: [42]
    )];

    public static TheoryData<SqlTestCase> WhereInCollectionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>
            WHERE <<dbo>>.<<Products>>.<<CategoryId>> IN (!!100, !!101, !!102)
            """
        ],
        expectedParameters: [10, 20, 30]
    )];
}