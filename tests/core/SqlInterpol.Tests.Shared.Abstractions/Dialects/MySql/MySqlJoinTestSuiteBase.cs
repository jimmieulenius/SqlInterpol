using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public abstract partial class MySqlJoinTestSuiteBase : IJoinTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> JoinTwoEntitiesData => [new SqlTestCase([
        """
        SELECT
            `dbo`.`Products`.`Id`,
            `OrderLine`.`OrderId`
        FROM `dbo`.`Products`
        JOIN `OrderLine`
            ON `dbo`.`Products`.`Id` = `OrderLine`.`ProductItemNumber`
        """
    ])];
}