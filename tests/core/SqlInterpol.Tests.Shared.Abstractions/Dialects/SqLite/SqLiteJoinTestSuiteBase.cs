using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteJoinTestSuiteBase : IJoinTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> JoinTwoEntitiesData => [new SqlTestCase([
        """
        SELECT
            "Products"."Id",
            "OrderLine"."OrderId"
        FROM "Products"
        JOIN "OrderLine"
            ON "Products"."Id" = "OrderLine"."ProductItemNumber"
        """
    ])];
}