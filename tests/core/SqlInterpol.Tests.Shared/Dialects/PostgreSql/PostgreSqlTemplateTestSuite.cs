using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlTemplateTestSuite : ITemplateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> TemplateSelectData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "o1"."Id", "o1"."CustomerId"
            FROM "dbo"."Orders" AS "o1"
            WHERE "o1"."CustomerId" = $1
            ORDER BY "o1"."Id" DESC
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateBulkInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders" ("Id")
            VALUES ($1), ($2)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders" SET "CustomerId" = $1 WHERE "Id" = $2;
            UPDATE "dbo"."Orders" SET "CustomerId" = $3 WHERE "Id" = $4
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            DELETE FROM "dbo"."Orders" WHERE "Id" = $1;
            DELETE FROM "dbo"."Orders" WHERE "Id" = $2
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO "dbo"."Orders"
            ("Id", "CustomerId")
            VALUES ($1, $2)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders"
            SET "CustomerId" = $1
            WHERE "Id" = $2
            """
        ]
    )];
}