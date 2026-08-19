using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlRawSqlTestSuite : IRawSqlTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> ComplexRawSqlData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
            FROM `dbo`.`Products`
            WHERE `dbo`.`Products`.`Price` > @p0
              AND p.Status = 'ACTIVE' /* Raw SQL condition */
            GROUP BY `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
            HAVING COUNT(*) > 1
            ORDER BY `dbo`.`Products`.`PROD_NAME` DESC
            LIMIT 10 OFFSET 5
            """
        ],
        expectedParameters: [50.00m]
    )];

    public static TheoryData<SqlTestCase> WindowFunctionData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                `dbo`.`Products`.`PROD_NAME`,
                `dbo`.`Products`.`Price`,
                AVG(`dbo`.`Products`.`Price`) OVER (PARTITION BY `dbo`.`Products`.`CategoryId`) AS `AvgCategoryPrice`
            FROM `dbo`.`Products`
            """
        ]
    )];
}