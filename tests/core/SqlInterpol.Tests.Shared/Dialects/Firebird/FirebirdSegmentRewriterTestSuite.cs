using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdSegmentRewriterTestSuite : ISegmentRewriterTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> SoftDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE  "dbo"."Orders"
             SET IsDeleted = 1
            WHERE "dbo"."Orders"."Id" = @p0
            """
        ],
        expectedParameters: [42]
    )];
}