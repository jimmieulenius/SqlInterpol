using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteUpsertTestSuite : IUpsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> UpsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" ("Id", "PROD_NAME", "CategoryId", "Price")
            VALUES (@p1, @p2, @p3, @p4)
            ON CONFLICT ("Id")
            DO UPDATE SET "PROD_NAME" = @p5, "Price" = @p6
            """
        ],
        expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
    )];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" (Id, Price)
            VALUES (@p1, @p2)
            ON DUPLICATE KEY UPDATE Price = @p3
            """
        ],
        expectedParameters: [1, 99.99m, 99.99m]
    )];

    public static TheoryData<SqlTestCase> OnConflictData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Products" (Id)
            VALUES (@p1)
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
            VALUES (@p1, @p2, @p3)
            ON CONFLICT ("Id")
            DO UPDATE SET "Age" = @p4, "Name" = @p5
            """
        ],
        expectedParameters: [32, 1, "Charlie", 32, "Charlie"]
    )];
}