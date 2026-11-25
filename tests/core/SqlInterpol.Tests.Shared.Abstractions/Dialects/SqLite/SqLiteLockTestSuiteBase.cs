using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteLockTestSuiteBase : ILockTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> SelectWithForUpdateData => 
    [
        new SqlTestCase(
            expectedExceptionType: typeof(SqlDialectException),
            expectedExceptionMessage: "The SQL dialect 'SqLite' does not support the operation or fragment type: 'FOR UPDATE'."
        )
    ];

    public static TheoryData<SqlTestCase> SelectWithForShareData => 
    [
        new SqlTestCase(
            expectedExceptionType: typeof(SqlDialectException),
            expectedExceptionMessage: "The SQL dialect 'SqLite' does not support the operation or fragment type: 'FOR SHARE'."
        )
    ];
}