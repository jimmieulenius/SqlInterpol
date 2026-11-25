using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteSelectAsTestSuiteBase : ISelectAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> ProjectionAsLiteralData => [new SqlTestCase([
        """
        SELECT
            "Products"."Id" AS "ProductId"
        FROM "Products"
        """
    ])];

    public static TheoryData<SqlTestCase> RawColumnAsProjectionData => [new SqlTestCase([
        """
        SELECT
            "Products"."Id" AS "ProductId"
        FROM "Products"
        """
    ])];

    public static TheoryData<SqlTestCase> ProjectionAsProjectionWithAttributeData => [new SqlTestCase([
        """
        SELECT
            "Products"."PROD_NAME" AS "Name"
        FROM "Products"
        """
    ])];
}