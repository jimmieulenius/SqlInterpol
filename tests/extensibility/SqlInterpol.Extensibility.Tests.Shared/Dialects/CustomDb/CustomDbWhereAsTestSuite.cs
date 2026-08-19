using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbWhereAsTestSuite : IWhereAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> WhereWithAliasedEntityData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                <<p>>.<<Id>> AS <<ProductId>>
            FROM <<dbo>>.<<Products>> AS <<p>>
            WHERE <<p>>.<<Id>> = !!100 AND <<p>>.<<CategoryId>> = !!101
            """
        ],
        expectedParameters: [1, 5]
    )];
}