using System;
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdUpdateTestSuite : IUpdateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> UpdateData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"order_status\" = @p0, \"Total\" = @p1\nWHERE \"dbo\".\"Orders\".\"Id\" = @p2"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateExplicitData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"dbo\".\"Orders\".\"order_status\" = @p0, \"dbo\".\"Orders\".\"Total\" = @p1\nWHERE \"dbo\".\"Orders\".\"Id\" = @p2"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateWithIgnoreData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Orders\"\nSET \"Id\" = @p0, \"order_status\" = @p1, \"Total\" = @p2\nWHERE \"dbo\".\"Orders\".\"Id\" = @p3"],
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
        expectedExceptionType: typeof(SqlDialectException)
    )];

    public static TheoryData<SqlTestCase> UpdateTemplateData => [new SqlTestCase(
        expectedSql: ["UPDATE \"Users\"\nSET \"Age\" = @p0, \"Name\" = @p1\nWHERE \"Users\".\"Id\" = @p2"],
        expectedParameters: [31, "Bob", 1]
    )];
}