using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteOrderByAsTestSuiteBase : IOrderByAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> OrderByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT *
        FROM "Products" AS "prod"
        ORDER BY
            "prod"."PROD_NAME" ASC
        """
    ])];
}