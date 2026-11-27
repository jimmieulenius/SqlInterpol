using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdUpsertTestSuiteBase : IUpsertTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> UpsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "Products" ("Id", "PROD_NAME", "CategoryId", "Price")
            VALUES (@p0, @p1, @p2, @p3)
            ON CONFLICT "Id"
            DO UPDATE SET "PROD_NAME" = @p4, "Price" = @p5
            """
        ],
        expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
    )];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "Products" (Id, Price)
            VALUES (@p0, @p1)
            ON DUPLICATE KEY UPDATE Price = @p2
            """
        ],
        expectedParameters: [1, 99.99m, 99.99m]
    )];

    public static TheoryData<SqlTestCase> OnConflictData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "Products" (Id)
            VALUES (@p0)
            ON CONFLICT DO NOTHING
            """
        ],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> UpsertTemplateData => [new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))];
}