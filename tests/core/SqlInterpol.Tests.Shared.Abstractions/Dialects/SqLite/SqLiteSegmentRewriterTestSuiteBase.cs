using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteSegmentRewriterTestSuiteBase : ISegmentRewriterTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> SoftDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE "Orders"
            SET IsDeleted = 1
            WHERE "Orders"."Id" = @p1
            """
        ],
        expectedParameters: [42]
    )];
}