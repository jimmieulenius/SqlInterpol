using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleInsertTestSuite : IInsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> InsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products"
            ("PROD_NAME", "CategoryId", "Price")
            VALUES (:0, :1, :2)
            """],
        expectedParameters: ["Test Product", 5, 19.99m]
    )];

    public static TheoryData<SqlTestCase> ManualInsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Orders"
            ("order_status", "Total")
            VALUES (:0, :1)
            """],
        expectedParameters: ["Manual", 100.00m]
    )];

    public static TheoryData<SqlTestCase> BulkInsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
            VALUES (:0, :1, :2), (:3, :4, :5)
            """],
        expectedParameters: ["Prod1", 1, 10m, "Prod2", 2, 20m]
    )];

    public static TheoryData<SqlTestCase> ReturningSingleData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
            VALUES (:0, :1, :2)
            RETURNING "Id"
            """],
        expectedParameters: ["Test Product", 5, 19.99m]
    )];

    public static TheoryData<SqlTestCase> ReturningMultipleData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
            VALUES (:0, :1, :2)
            RETURNING "Id", "PROD_NAME"
            """],
        expectedParameters: ["Test Product", 5, 19.99m]
    )];

    public static TheoryData<SqlTestCase> InsertWithIgnoreData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
            VALUES (:0, :1)
            """],
        expectedParameters: [1, "Gadget"]
    )];

    public static TheoryData<SqlTestCase> InsertComplexData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "tbl_complex_products"
            ("Id", "Name", "Status", "Category")
            VALUES (:0, :1, :2, :3);
            """],
        expectedParameters: [42, "Mechanical Keyboard", 1, "Electronics"]
    )];

    public static TheoryData<SqlTestCase> InsertTemplateData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "Users"
            ("Age", "Id", "Name")
            VALUES (:0, :1, :2)
            """],
        expectedParameters: [30, 1, "Alice"]
    )];
}