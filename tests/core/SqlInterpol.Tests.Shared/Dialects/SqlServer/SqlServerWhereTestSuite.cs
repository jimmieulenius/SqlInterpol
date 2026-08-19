using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerWhereTestSuite : IWhereTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> WhereSimpleParameterData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                [dbo].[Products].[Id]
            FROM [dbo].[Products]
            WHERE [dbo].[Products].[Id] = @p0
            """
        ],
        expectedParameters: [42]
    )];

    public static TheoryData<SqlTestCase> WhereInCollectionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                [dbo].[Products].[Id]
            FROM [dbo].[Products]
            WHERE [dbo].[Products].[CategoryId] IN (@p0, @p1, @p2)
            """
        ],
        expectedParameters: [10, 20, 30]
    )];
}