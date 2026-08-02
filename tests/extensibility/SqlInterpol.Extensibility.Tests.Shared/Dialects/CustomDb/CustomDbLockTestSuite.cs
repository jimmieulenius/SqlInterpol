using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using SqlInterpol.Extensibility;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbLockTestSuite : ILockTestSuite
{
    public SqlBuilder CreateBuilder() => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb();
#else
        SqlBuilderFactory.CustomDb();
#endif

    public static TheoryData<SqlTestCase> SelectWithForUpdateData => 
    [
        new SqlTestCase(
            expectedExceptionType: typeof(SqlDialectException),
            expectedExceptionMessage: "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'FOR UPDATE'."
        )
    ];

    public static TheoryData<SqlTestCase> SelectWithForShareData => 
    [
        new SqlTestCase(
            expectedExceptionType: typeof(SqlDialectException),
            expectedExceptionMessage: "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'FOR SHARE'."
        )
    ];
}