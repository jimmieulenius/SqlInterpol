using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlOrderByAsTestSuite : IOrderByAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> OrderByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT *
        FROM `dbo`.`Products` AS `prod`
        ORDER BY
            `prod`.`PROD_NAME` ASC
        """
    ])];
}