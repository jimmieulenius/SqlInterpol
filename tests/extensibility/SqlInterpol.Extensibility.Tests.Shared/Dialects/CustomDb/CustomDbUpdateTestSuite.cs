using System;
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbUpdateTestSuite : IUpdateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> UpdateData => [new SqlTestCase(
        expectedSql: ["UPDATE <<dbo>>.<<Orders>>\nSET <<order_status>> = !!100, <<Total>> = !!101\nWHERE <<dbo>>.<<Orders>>.<<Id>> = !!102"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateExplicitData => [new SqlTestCase(
        expectedSql: ["UPDATE <<dbo>>.<<Orders>>\nSET <<dbo>>.<<Orders>>.<<order_status>> = !!100, <<dbo>>.<<Orders>>.<<Total>> = !!101\nWHERE <<dbo>>.<<Orders>>.<<Id>> = !!102"],
        expectedParameters: ["Shipped", 99.99m, 42]
    )];

    public static TheoryData<SqlTestCase> UpdateWithIgnoreData => [new SqlTestCase(
        expectedSql: ["UPDATE <<dbo>>.<<Orders>>\nSET <<Id>> = !!100, <<order_status>> = !!101, <<Total>> = !!102\nWHERE <<dbo>>.<<Orders>>.<<Id>> = !!103"],
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
        expectedSql: ["UPDATE <<Users>>\nSET <<Age>> = !!100, <<Name>> = !!101\nWHERE <<Users>>.<<Id>> = !!102"],
        expectedParameters: [31, "Bob", 1]
    )];
}