using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlJoinSubqueryTestSuite : IJoinSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> JoinSubqueryData => [new SqlTestCase([
        """
        SELECT 
            `c`.`Name`, 
            `stats`.`TotalPrice`
        FROM `Category` AS `c`
        LEFT JOIN
        (
            SELECT 
                `p`.`CategoryId` AS `CategoryId`,
                SUM(`p`.`Price`) AS `TotalPrice`
            FROM `dbo`.`Products` AS `p`
            GROUP BY `p`.`CategoryId`
        ) AS `stats`
            ON `stats`.`CategoryId` = `c`.`Id`
        """
    ])];
}