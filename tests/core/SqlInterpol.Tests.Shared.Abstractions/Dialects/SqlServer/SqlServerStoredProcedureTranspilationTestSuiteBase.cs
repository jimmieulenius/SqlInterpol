using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public abstract partial class SqlServerStoredProcedureTranspilationTestSuiteBase : IStoredProcedureTranspilationTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> CallData => new()
    {
        new SqlTestCase(
            expectedSql:
            [
                """
                EXEC process_order @p0, @p1
                """
            ],
            expectedParameters: [1, "Active"]
        )
    };
}
