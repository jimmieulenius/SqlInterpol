using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleSelectAsTestSuite : ISelectAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> ProjectionAsLiteralData => [new SqlTestCase([
        """
        SELECT
            "dbo"."Products"."Id" AS "ProductId"
        FROM "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> RawColumnAsProjectionData => [new SqlTestCase([
        """
        SELECT
            "dbo"."Products"."Id" AS "ProductId"
        FROM "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> ProjectionAsProjectionWithAttributeData => [new SqlTestCase([
        """
        SELECT
            "dbo"."Products"."PROD_NAME" AS "Name"
        FROM "dbo"."Products"
        """
    ])];
}