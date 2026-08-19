using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlGroupByAsTestSuite : IGroupByAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> GroupByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT `prod`.`PROD_NAME`, `prod`.`IsActive`, COUNT(*)
        FROM `dbo`.`Products` AS `prod`
        GROUP BY `prod`.`PROD_NAME`, `prod`.`IsActive`
        """
    ])];
}