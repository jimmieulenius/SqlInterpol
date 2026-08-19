using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlCustomParserTestSuite : ICustomParserTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20, 30];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> CustomParserData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN (@p0, @p1, @p2)
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}