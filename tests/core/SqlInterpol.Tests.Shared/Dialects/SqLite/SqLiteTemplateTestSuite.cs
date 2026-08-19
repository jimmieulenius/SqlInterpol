using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteTemplateTestSuite : ITemplateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> TemplateSelectData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "o1"."Id", "o1"."CustomerId"
            FROM "dbo"."Orders" AS "o1"
            WHERE "o1"."CustomerId" = @p1
            ORDER BY "o1"."Id" DESC
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateBulkInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders" ("Id")
            VALUES (@p1), (@p2)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders" SET "CustomerId" = @p1 WHERE "Id" = @p2;
            UPDATE "dbo"."Orders" SET "CustomerId" = @p3 WHERE "Id" = @p4
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            DELETE FROM "dbo"."Orders" WHERE "Id" = @p1;
            DELETE FROM "dbo"."Orders" WHERE "Id" = @p2
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders"
            ("Id", "CustomerId")
            VALUES (@p1, @p2)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders"
            SET "CustomerId" = @p1
            WHERE "Id" = @p2
            """
        ]
    )];
}