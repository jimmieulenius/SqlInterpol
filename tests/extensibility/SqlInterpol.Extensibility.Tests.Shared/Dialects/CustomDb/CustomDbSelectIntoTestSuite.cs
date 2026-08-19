using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbSelectIntoTestSuite : ISelectIntoTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> SelectIntoData => [
        new SqlTestCase(
            expectedExceptionType: typeof(SqlDialectException),
            expectedExceptionMessage: "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'SELECT INTO'."
        )
    ];

    public static TheoryData<SqlTestCase> SelectIntoParameterizedData => [
        new SqlTestCase(
            expectedExceptionType: typeof(SqlDialectException),
            expectedExceptionMessage: "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'SELECT INTO'."
        )
    ];
}