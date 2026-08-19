using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleOrderByAsTestSuite : IOrderByAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> OrderByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Products" "prod"
        ORDER BY
            "prod"."PROD_NAME" ASC
        """
    ])];
}