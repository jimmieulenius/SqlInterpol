using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerSetOperationTestSuite : ISetOperationTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> QueryIntersectData => [new SqlTestCase(
        expectedSql: ["SELECT [dbo].[Products].[Id] FROM [dbo].[Products]\nINTERSECT\nSELECT [dbo].[Products].[Id] FROM [dbo].[Products] WHERE [dbo].[Products].[CategoryId] = @p0"],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> QueryExceptData => [new SqlTestCase(
        expectedSql: ["SELECT [dbo].[Products].[Id] FROM [dbo].[Products]\nEXCEPT\nSELECT [dbo].[Products].[Id] FROM [dbo].[Products] WHERE [dbo].[Products].[CategoryId] = @p0"],
        expectedParameters: [2]
    )];

    public static TheoryData<SqlTestCase> Select_UnionAllData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
            FROM [dbo].[Products]
            WHERE [dbo].[Products].[CategoryId] = @p0
            UNION ALL
            SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
            FROM [dbo].[Products]
            WHERE [dbo].[Products].[CategoryId] = @p1
            """
        ],
        expectedParameters: [1, 2]
    )];
}