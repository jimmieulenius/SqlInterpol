using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbInsertTestSuite : IInsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> InsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO <<dbo>>.<<Products>>
            (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
            VALUES (!!100, !!101, !!102)
            """],
        expectedParameters: ["Test Product", 5, 19.99m]
    )];

    public static TheoryData<SqlTestCase> ManualInsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO <<dbo>>.<<Orders>>
            (<<order_status>>, <<Total>>)
            VALUES (!!100, !!101)
            """],
        expectedParameters: ["Manual", 100.00m]
    )];

    public static TheoryData<SqlTestCase> BulkInsertData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO <<dbo>>.<<Products>> (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
            VALUES (!!100, !!101, !!102), (!!103, !!104, !!105)
            """],
        expectedParameters: ["Prod1", 1, 10m, "Prod2", 2, 20m]
    )];

    public static TheoryData<SqlTestCase> ReturningSingleData => [new SqlTestCase(
        expectedExceptionType: typeof(SqlDialectException),
        expectedExceptionMessage: "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'RETURNING'."
    )];

    public static TheoryData<SqlTestCase> ReturningMultipleData => [new SqlTestCase(
        expectedExceptionType: typeof(SqlDialectException),
        expectedExceptionMessage: "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'RETURNING'."
    )];

    public static TheoryData<SqlTestCase> InsertWithIgnoreData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO <<dbo>>.<<Products>> (<<Id>>, <<PROD_NAME>>)
            VALUES (!!100, !!101)
            """],
        expectedParameters: [1, "Gadget"]
    )];

    public static TheoryData<SqlTestCase> InsertComplexData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO <<tbl_complex_products>>
            (<<Id>>, <<Name>>, <<Status>>, <<Category>>)
            VALUES (!!100, !!101, !!102, !!103);
            """],
        expectedParameters: [42, "Mechanical Keyboard", 1, "Electronics"]
    )];

    public static TheoryData<SqlTestCase> InsertTemplateData => [new SqlTestCase(
        expectedSql: ["""
            INSERT INTO <<Users>>
            (<<Age>>, <<Id>>, <<Name>>)
            VALUES (!!100, !!101, !!102)
            """],
        expectedParameters: [30, 1, "Alice"]
    )];
}