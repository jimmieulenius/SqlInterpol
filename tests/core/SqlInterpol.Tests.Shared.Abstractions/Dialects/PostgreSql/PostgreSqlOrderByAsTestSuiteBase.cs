using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public abstract partial class PostgreSqlOrderByAsTestSuiteBase : IOrderByAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> OrderByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Products" AS "prod"
        ORDER BY
            "prod"."PROD_NAME" ASC
        """
    ])];
}