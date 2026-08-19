using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlCustomParserTestSuite : ICustomParserTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20, 30];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> CustomParserData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN ($1, $2, $3)
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}