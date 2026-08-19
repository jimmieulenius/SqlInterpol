using System;
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleUpdateTestSuite : IUpdateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> UpdateData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"order_status\" = :0, \"Total\" = :1\nWHERE \"dbo\".\"Orders\".\"Id\" = :2"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateExplicitData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"dbo\".\"Orders\".\"order_status\" = :0, \"dbo\".\"Orders\".\"Total\" = :1\nWHERE \"dbo\".\"Orders\".\"Id\" = :2"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateWithIgnoreData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"Id\" = :0, \"order_status\" = :1, \"Total\" = :2\nWHERE \"dbo\".\"Orders\".\"Id\" = :3"],
        expectedParameters: [42, "Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateInvalidEntityData => [new SqlTestCase(
        expectedExceptionType: typeof(ArgumentException),
        expectedExceptionMessage: "Entity must implement ISqlEntityBase<T>."
    )];

    public static TheoryData<SqlTestCase> UpdateInvalidEntityPropertyData => [new SqlTestCase(
        expectedExceptionType: typeof(ArgumentException),
        expectedExceptionMessage: "Property 'NonExistentProperty' on DTO does not exist on Entity."
    )];

    public static TheoryData<SqlTestCase> MultiTableUpdateData => [new SqlTestCase(
        expectedSql: ["MERGE INTO \"dbo\".\"Products\"\nUSING \"Category\" \"c1\"\nON (\"dbo\".\"Products\".\"CategoryId\" = c1.Id)\nWHEN MATCHED THEN UPDATE SET \"Price\" = :0"],
        expectedParameters: [10]
    )];

    public static TheoryData<SqlTestCase> UpdateTemplateData => [new SqlTestCase(
        expectedSql: ["UPDATE \"Users\"\nSET \"Age\" = :0, \"Name\" = :1\nWHERE \"Users\".\"Id\" = :2"],
        expectedParameters: [31, "Bob", 1]
    )];
}