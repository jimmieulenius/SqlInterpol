using System;
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlUpdateTestSuite : IUpdateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> UpdateData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"order_status\" = $1, \"Total\" = $2\nWHERE \"dbo\".\"Orders\".\"Id\" = $3"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateExplicitData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"dbo\".\"Orders\".\"order_status\" = $1, \"dbo\".\"Orders\".\"Total\" = $2\nWHERE \"dbo\".\"Orders\".\"Id\" = $3"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateWithIgnoreData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"Id\" = $1, \"order_status\" = $2, \"Total\" = $3\nWHERE \"dbo\".\"Orders\".\"Id\" = $4"],
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
        expectedSql: ["UPDATE \"dbo\".\"Products\"\nSET \"Price\" = $1\nFROM \"Category\" AS \"c1\"\nWHERE \"dbo\".\"Products\".\"CategoryId\" = c1.Id"],
        expectedParameters: [10]
    )];

    public static TheoryData<SqlTestCase> UpdateTemplateData => [new SqlTestCase(
        expectedSql: ["UPDATE \"Users\"\nSET \"Age\" = $1, \"Name\" = $2\nWHERE \"Users\".\"Id\" = $3"],
        expectedParameters: [31, "Bob", 1]
    )];
}