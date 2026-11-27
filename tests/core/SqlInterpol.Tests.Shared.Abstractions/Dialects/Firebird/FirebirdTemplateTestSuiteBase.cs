using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdTemplateTestSuiteBase : ITemplateTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> TemplateSelectData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "o1"."Id", "o1"."CustomerId"
            FROM "Orders" AS "o1"
            WHERE "o1"."CustomerId" = @p0
            ORDER BY "o1"."Id" DESC
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateBulkInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "Orders" ("Id")
            VALUES (@p0), (@p1)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "Orders" SET "CustomerId" = @p0 WHERE "Id" = @p1;
            UPDATE "Orders" SET "CustomerId" = @p2 WHERE "Id" = @p3
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            DELETE FROM "Orders" WHERE "Id" = @p0;
            DELETE FROM "Orders" WHERE "Id" = @p1
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "Orders"
            ("Id", "CustomerId")
            VALUES (@p0, @p1)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "Orders"
            SET "CustomerId" = @p0
            WHERE "Id" = @p1
            """
        ]
    )];
}