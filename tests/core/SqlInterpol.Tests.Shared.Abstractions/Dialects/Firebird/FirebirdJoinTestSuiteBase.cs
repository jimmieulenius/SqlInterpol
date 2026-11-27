using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdJoinTestSuiteBase : IJoinTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

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