using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdDeleteAsTestSuite : IDeleteAsTestSuite
{
    private static readonly object[] _expectedParameters = [42];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> DeleteWithExplicitAliasData =>
    [
        new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))
    ];

    public static TheoryData<SqlTestCase> DeleteWithoutAliasData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = @p0
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}