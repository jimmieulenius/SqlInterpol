using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteInsertTestSuite : IInsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> InsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products"
            ("PROD_NAME", "CategoryId", "Price")
            VALUES (@p1, @p2, @p3)
            """],
        expectedParameters: ["Test Product", 5, 19.99m]
    )];

    public static TheoryData<SqlTestCase> ManualInsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Orders"
            ("order_status", "Total")
            VALUES (@p1, @p2)
            """],
        expectedParameters: ["Manual", 100.00m]
    )];

    public static TheoryData<SqlTestCase> BulkInsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
            VALUES (@p1, @p2, @p3), (@p4, @p5, @p6)
            """],
        expectedParameters: ["Prod1", 1, 10m, "Prod2", 2, 20m]
    )];

    public static TheoryData<SqlTestCase> ReturningSingleData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
            VALUES (@p1, @p2, @p3)
            RETURNING "Id"
            """],
        expectedParameters: ["Test Product", 5, 19.99m]
    )];

    public static TheoryData<SqlTestCase> ReturningMultipleData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
            VALUES (@p1, @p2, @p3)
            RETURNING "Id", "PROD_NAME"
            """],
        expectedParameters: ["Test Product", 5, 19.99m]
    )];

    public static TheoryData<SqlTestCase> InsertWithIgnoreData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
            VALUES (@p1, @p2)
            """],
        expectedParameters: [1, "Gadget"]
    )];

    public static TheoryData<SqlTestCase> InsertComplexData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "tbl_complex_products"
            ("Id", "Name", "Status", "Category")
            VALUES (@p1, @p2, @p3, @p4);
            """],
        expectedParameters: [42, "Mechanical Keyboard", 1, "Electronics"]
    )];

    public static TheoryData<SqlTestCase> InsertTemplateData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO "Users"
            ("Age", "Id", "Name")
            VALUES (@p1, @p2, @p3)
            """],
        expectedParameters: [30, 1, "Alice"]
    )];
}