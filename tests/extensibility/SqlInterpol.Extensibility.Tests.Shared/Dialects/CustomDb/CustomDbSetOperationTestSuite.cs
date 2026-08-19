using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbSetOperationTestSuite : ISetOperationTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> QueryIntersectData => [new SqlTestCase(
        expectedSql: ["SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>\nINTERSECT\nSELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>> WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100"],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> QueryExceptData => [new SqlTestCase(
        expectedSql: ["SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>\nEXCEPT\nSELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>> WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100"],
        expectedParameters: [2]
    )];

    public static TheoryData<SqlTestCase> Select_UnionAllData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
            FROM <<dbo>>.<<Products>>
            WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
            UNION ALL
            SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
            FROM <<dbo>>.<<Products>>
            WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!101
            """
        ],
        expectedParameters: [1, 2]
    )];
}