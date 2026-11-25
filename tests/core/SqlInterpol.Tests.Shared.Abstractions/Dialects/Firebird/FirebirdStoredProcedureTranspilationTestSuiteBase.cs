using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdStoredProcedureTranspilationTestSuiteBase : IStoredProcedureTranspilationTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> CallData => new()
    {
        new SqlTestCase(
            expectedSql:
            [
                """
                CALL process_order(@p0, @p1)
                """
            ],
            expectedParameters: [1, "Active"]
        )
    };
}
