using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteStoredProcedureTranspilationTestSuite : IStoredProcedureTranspilationTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> CallData => new()
    {
        new SqlTestCase(
            expectedSql:
            [
                """
                CALL process_order(@p1, @p2)
                """
            ],
            expectedParameters: [1, "Active"]
        )
    };
}
