using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlWhereAsTestSuite : IWhereAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> WhereWithAliasedEntityData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT
                `p`.`Id` AS `ProductId`
            FROM `dbo`.`Products` AS `p`
            WHERE `p`.`Id` = @p0 AND `p`.`CategoryId` = @p1
            """
        ],
        expectedParameters: [1, 5]
    )];
}