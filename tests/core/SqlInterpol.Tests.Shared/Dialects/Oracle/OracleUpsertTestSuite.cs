using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleUpsertTestSuite : IUpsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> UpsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" ("Id", "PROD_NAME", "CategoryId", "Price")
            VALUES (:0, :1, :2, :3)
            ON CONFLICT "Id"
            DO UPDATE SET "PROD_NAME" = :4, "Price" = :5
            """
        ],
        expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
    )];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" (Id, Price)
            VALUES (:0, :1)
            ON DUPLICATE KEY UPDATE Price = :2
            """
        ],
        expectedParameters: [1, 99.99m, 99.99m]
    )];

    public static TheoryData<SqlTestCase> OnConflictData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" (Id)
            VALUES (:0)
            ON CONFLICT DO NOTHING
            """
        ],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> UpsertTemplateData => [new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))];
}