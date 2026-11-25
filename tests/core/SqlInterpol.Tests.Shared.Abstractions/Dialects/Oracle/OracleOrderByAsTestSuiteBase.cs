using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleOrderByAsTestSuiteBase : IOrderByAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> OrderByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Products" "prod"
        ORDER BY
            "prod"."PROD_NAME" ASC
        """
    ])];
}