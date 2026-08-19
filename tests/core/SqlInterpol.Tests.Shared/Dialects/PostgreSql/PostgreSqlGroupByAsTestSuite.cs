using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlGroupByAsTestSuite : IGroupByAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> GroupByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT "prod"."PROD_NAME", "prod"."IsActive", COUNT(*)
        FROM "dbo"."Products" AS "prod"
        GROUP BY "prod"."PROD_NAME", "prod"."IsActive"
        """
    ])];
}