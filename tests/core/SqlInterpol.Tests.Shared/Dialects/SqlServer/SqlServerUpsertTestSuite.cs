using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerUpsertTestSuite : IUpsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> UpsertData => [new SqlTestCase(
        expectedSql: [
            """
            MERGE INTO [dbo].[Products] AS target
            USING (VALUES (@p0, @p1, @p2, @p3)) AS source([Id], [PROD_NAME], [CategoryId], [Price])
            ON target.[Id] = source.[Id]
            WHEN MATCHED THEN
              UPDATE SET target.[PROD_NAME] = @p4, target.[Price] = @p5
            WHEN NOT MATCHED THEN
              INSERT ([Id], [PROD_NAME], [CategoryId], [Price])
              VALUES (source.[Id], source.[PROD_NAME], source.[CategoryId], source.[Price]);
            """
        ],
        expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
    )];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData => [new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))];

    public static TheoryData<SqlTestCase> OnConflictData => [new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))];

    public static TheoryData<SqlTestCase> UpsertTemplateData => [new SqlTestCase(
        expectedSql: [
            """
            MERGE INTO [Users] AS target
            USING (VALUES (@p0, @p1, @p2)) AS source([Age], [Id], [Name])
            ON target.[Id] = source.[Id]
            WHEN MATCHED THEN
              UPDATE SET target.[Age] = @p3, target.[Name] = @p4
            WHEN NOT MATCHED THEN
              INSERT ([Age], [Id], [Name])
              VALUES (source.[Age], source.[Id], source.[Name]);
            """
        ],
        expectedParameters: [32, 1, "Charlie", 32, "Charlie"]
    )];
}