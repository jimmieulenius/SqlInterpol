using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlSegmentRewriterTestSuite : ISegmentRewriterTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> SoftDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE  "dbo"."Orders"
             SET IsDeleted = 1
            WHERE "dbo"."Orders"."Id" = $1
            """
        ],
        expectedParameters: [42]
    )];
}