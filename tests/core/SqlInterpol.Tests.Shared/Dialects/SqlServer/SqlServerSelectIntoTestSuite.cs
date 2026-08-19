using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerSelectIntoTestSuite : ISelectIntoTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> SelectIntoData => [new SqlTestCase([
        """
        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
        INTO #TempProducts
        FROM [dbo].[Products]
        """
    ])];

    public static TheoryData<SqlTestCase> SelectIntoParameterizedData => [new SqlTestCase([
        """
        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
        INTO #TempProducts
        FROM [dbo].[Products]
        """
    ])];
}