using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public abstract partial class SqlServerUpdateAsTestSuiteBase : IUpdateAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE [ord]
            SET [order_status] = @p0, [Total] = @p1
            FROM [dbo].[Orders] AS [ord]
            WHERE [ord].[Id] = 1
            """
        ],
        expectedParameters: ["Shipped", 99.99m]
    )];
}