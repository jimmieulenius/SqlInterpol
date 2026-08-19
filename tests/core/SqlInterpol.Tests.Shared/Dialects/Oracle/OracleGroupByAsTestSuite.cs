using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleGroupByAsTestSuite : IGroupByAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> GroupByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT "prod"."PROD_NAME", "prod"."IsActive", COUNT(*)
        FROM "dbo"."Products" "prod"
        GROUP BY "prod"."PROD_NAME", "prod"."IsActive"
        """
    ])];
}