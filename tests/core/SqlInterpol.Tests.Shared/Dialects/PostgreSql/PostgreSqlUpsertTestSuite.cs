using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlUpsertTestSuite : IUpsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> UpsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" ("Id", "PROD_NAME", "CategoryId", "Price")
            VALUES ($1, $2, $3, $4)
            ON CONFLICT ("Id")
            DO UPDATE SET "PROD_NAME" = $5, "Price" = $6
            """
        ],
        expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
    )];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" (Id, Price)
            VALUES ($1, $2)
            ON DUPLICATE KEY UPDATE Price = $3
            """
        ],
        expectedParameters: [1, 99.99m, 99.99m]
    )];

    public static TheoryData<SqlTestCase> OnConflictData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" (Id)
            VALUES ($1)
            ON CONFLICT DO NOTHING
            """
        ],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> UpsertTemplateData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "Users"
            ("Age", "Id", "Name")
            VALUES ($1, $2, $3)
            ON CONFLICT ("Id")
            DO UPDATE SET "Age" = $4, "Name" = $5
            """
        ],
        expectedParameters: [32, 1, "Charlie", 32, "Charlie"]
    )];
}