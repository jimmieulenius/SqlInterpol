using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleSegmentRewriterTestSuiteBase : ISegmentRewriterTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> SoftDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "dbo"."Orders"
            SET IsDeleted = 1
            WHERE "dbo"."Orders"."Id" = :0
            """
        ],
        expectedParameters: [42]
    )];
}