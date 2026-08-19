using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlFromSubqueryTestSuite : IFromSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> From_SubqueryData => [new SqlTestCase([
        """
        SELECT
            `c`.`Name`,
            `stats`.`TotalPrice`
        FROM
        (
            SELECT
                `p`.`CategoryId` AS `CategoryId`,
                SUM(`p`.`Price`) AS `TotalPrice`
            FROM `dbo`.`Products` AS `p`
            GROUP BY `p`.`CategoryId`
        ) AS `stats`
        JOIN `Category` AS `c` ON `stats`.`CategoryId` = `c`.`Id`
        """
    ])];
}