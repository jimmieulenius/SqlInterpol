using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbUpsertTestSuite : IUpsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> UpsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO <<dbo>>.<<Products>> (<<Id>>, <<PROD_NAME>>, <<CategoryId>>, <<Price>>)
            VALUES (!!100, !!101, !!102, !!103)
            ON CONFLICT <<Id>>
            DO UPDATE SET <<PROD_NAME>> = !!104, <<Price>> = !!105
            """
        ],
        expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
    )];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO <<dbo>>.<<Products>> (Id, Price)
            VALUES (!!100, !!101)
            ON DUPLICATE KEY UPDATE Price = !!102
            """
        ],
        expectedParameters: [1, 99.99m, 99.99m]
    )];

    public static TheoryData<SqlTestCase> OnConflictData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO <<dbo>>.<<Products>> (Id)
            VALUES (!!100)
            ON CONFLICT DO NOTHING
            """
        ],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> UpsertTemplateData => [new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))];
}