using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerDeleteAsTestSuite : IDeleteAsTestSuite
{
    private static readonly object[] _expectedParameters = [42];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> DeleteWithExplicitAliasData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE [o] FROM [dbo].[Orders] AS [o]
                WHERE [o].[Id] = @p0
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];

    public static TheoryData<SqlTestCase> DeleteWithoutAliasData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM [dbo].[Orders]
                WHERE [dbo].[Orders].[Id] = @p0
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}