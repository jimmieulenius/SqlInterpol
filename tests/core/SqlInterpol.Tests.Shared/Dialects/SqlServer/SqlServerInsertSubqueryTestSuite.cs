using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public partial class SqlServerInsertSubqueryTestSuite : IInsertSubqueryTestSuite
{
    private static readonly object[] _expectedParameters = [100];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> InsertSelectData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO [dbo].[Orders] 
            ([Id], [Total])
            SELECT [OrderLine].[OrderId], [OrderLine].[Quantity]
            FROM [OrderLine]
            WHERE [OrderLine].[OrderId] = @p0
            """
        ],
        expectedParameters: _expectedParameters
    )];
}