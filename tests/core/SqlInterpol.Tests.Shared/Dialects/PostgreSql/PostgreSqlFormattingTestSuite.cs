using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlFormattingTestSuite : IFormattingTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> Select_WithNewLinesData => [new SqlTestCase([
        """
        SELECT 
            "dbo"."Products"."Id", 
            "dbo"."Products"."PROD_NAME"
        FROM 
            "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithTabsData => [new SqlTestCase([
        """
        SELECT  "dbo"."Products"."Id",  "dbo"."Products"."PROD_NAME"
        FROM  "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithExtraSpacesData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id"
          FROM "dbo"."Products"
         WHERE "dbo"."Products"."Id" = 1
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithMixedWhitespaceData => [new SqlTestCase([
        """

            SELECT "dbo"."Products"."Id"
            FROM "dbo"."Products"

        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithCommentsData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id" -- This is the primary key
        FROM "dbo"."Products" /* This is the table */
        """
    ])];

    public static TheoryData<SqlTestCase> InsertVerticalLayoutData => [new SqlTestCase([
        """
        INSERT INTO "dbo"."Orders"
        (
            "order_status",
            "Total"
        )
        VALUES
        (
            $1,
            $2
        )
        """
    ])];

    public static TheoryData<SqlTestCase> UpdateVerticalLayoutData => [new SqlTestCase([
        """
        UPDATE "dbo"."Orders"
        SET
            "order_status" = $1,
            "Total" = $2
        """
    ])];

    public static TheoryData<SqlTestCase> BulkInsertVerticalLayoutData => [new SqlTestCase([
        """
        INSERT INTO "dbo"."Products"
        (
            "PROD_NAME",
            "CategoryId",
            "Price"
        )
        VALUES
        (
            $1,
            $2,
            $3
        ),
        (
            $4,
            $5,
            $6
        )
        """
    ])];

    public static TheoryData<SqlTestCase> WhereInVerticalLayoutData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Orders"
        WHERE "dbo"."Orders"."Id" IN (
            $1,
            $2,
            $3
        )
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByEnumerableVerticalLayoutData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Orders"
        ORDER BY 
            "dbo"."Orders"."Total",
            "dbo"."Orders"."Id" DESC
        """
    ])];

    public static TheoryData<SqlTestCase> SelectEntityExpansionVerticalLayoutData => [new SqlTestCase([
        """
        SELECT
            "p1"."Id",
            "p1"."PROD_NAME"
        FROM "dbo"."Products" AS "p1"
        """
    ])];
}