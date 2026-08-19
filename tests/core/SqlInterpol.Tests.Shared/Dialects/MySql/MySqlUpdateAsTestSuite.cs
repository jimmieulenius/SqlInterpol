using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlUpdateAsTestSuite : IUpdateAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE `ord`, `dbo`.`Orders` AS `ord`
            SET `order_status` = @p0, `Total` = @p1
            WHERE `ord`.`Id` = 1
            """
        ],
        expectedParameters: ["Shipped", 99.99m]
    )];
}