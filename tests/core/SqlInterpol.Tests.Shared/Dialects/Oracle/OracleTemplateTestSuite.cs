using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleTemplateTestSuite : ITemplateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> TemplateSelectData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "o1"."Id", "o1"."CustomerId"
            FROM "dbo"."Orders" "o1"
            WHERE "o1"."CustomerId" = :0
            ORDER BY "o1"."Id" DESC
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateBulkInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders" ("Id")
            VALUES (:0), (:1)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders" SET "CustomerId" = :0 WHERE "Id" = :1;
            UPDATE "dbo"."Orders" SET "CustomerId" = :2 WHERE "Id" = :3
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            DELETE FROM "dbo"."Orders" WHERE "Id" = :0;
            DELETE FROM "dbo"."Orders" WHERE "Id" = :1
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders"
            ("Id", "CustomerId")
            VALUES (:0, :1)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders"
            SET "CustomerId" = :0
            WHERE "Id" = :1
            """
        ]
    )];
}