using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public abstract partial class MySqlStoredProcedureTranspilationTestSuiteBase : IStoredProcedureTranspilationTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

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
