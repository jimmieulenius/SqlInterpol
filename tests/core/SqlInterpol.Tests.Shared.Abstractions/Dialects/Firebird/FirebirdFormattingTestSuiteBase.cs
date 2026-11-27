using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdFormattingTestSuiteBase : IFormattingTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> Select_WithNewLinesData => [new SqlTestCase([
        """
        SELECT 
            "Products"."Id", 
            "Products"."PROD_NAME"
        FROM 
            "Products"
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithTabsData => [new SqlTestCase([
        """
        SELECT  "Products"."Id",  "Products"."PROD_NAME"
        FROM  "Products"
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithExtraSpacesData => [new SqlTestCase([
        """
        SELECT "Products"."Id"
          FROM "Products"
         WHERE "Products"."Id" = 1
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithMixedWhitespaceData => [new SqlTestCase([
        """

            SELECT "Products"."Id"
            FROM "Products"

        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithCommentsData => [new SqlTestCase([
        """
        SELECT "Products"."Id" -- This is the primary key
        FROM "Products" /* This is the table */
        """
    ])];

    public static TheoryData<SqlTestCase> InsertVerticalLayoutData => [new SqlTestCase([
        """
        INSERT INTO "Orders"
        (
            "order_status",
            "Total"
        )
        VALUES
        (
            @p0,
            @p1
        )
        """
    ])];

    public static TheoryData<SqlTestCase> UpdateVerticalLayoutData => [new SqlTestCase([
        """
        UPDATE "Orders"
        SET
            "order_status" = @p0,
            "Total" = @p1
        """
    ])];

    public static TheoryData<SqlTestCase> BulkInsertVerticalLayoutData => [new SqlTestCase([
        """
        INSERT INTO "Products"
        (
            "PROD_NAME",
            "CategoryId",
            "Price"
        )
        VALUES
        (
            @p0,
            @p1,
            @p2
        ),
        (
            @p3,
            @p4,
            @p5
        )
        """
    ])];

    public static TheoryData<SqlTestCase> WhereInVerticalLayoutData => [new SqlTestCase([
        """
        SELECT *
        FROM "Orders"
        WHERE "Orders"."Id" IN (
            @p0,
            @p1,
            @p2
        )
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByEnumerableVerticalLayoutData => [new SqlTestCase([
        """
        SELECT *
        FROM "Orders"
        ORDER BY 
            "Orders"."Total",
            "Orders"."Id" DESC
        """
    ])];

    public static TheoryData<SqlTestCase> SelectEntityExpansionVerticalLayoutData => [new SqlTestCase([
        """
        SELECT
            "p1"."Id",
            "p1"."PROD_NAME"
        FROM "Products" AS "p1"
        """
    ])];
}