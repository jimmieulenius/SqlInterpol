using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteCustomParserTestSuite : ICustomParserTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20, 30];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> CustomParserData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN (@p1, @p2, @p3)
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}