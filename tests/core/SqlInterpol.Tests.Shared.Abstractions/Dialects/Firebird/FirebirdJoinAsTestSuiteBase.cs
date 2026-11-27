using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdJoinAsTestSuiteBase : IJoinAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> JoinWithLiteralAliasesData => [new SqlTestCase([
        """
        SELECT
            "p"."Id",
            "ol"."OrderId"
        FROM "Products" AS "p"
        JOIN "OrderLine" AS "ol"
            ON "p"."Id" = "ol"."ProductItemNumber"
        """
    ])];

    public static TheoryData<SqlTestCase> JoinWithExplicitApiAliasesData => [new SqlTestCase([
        """
        SELECT
            "prod"."Id",
            "OrderLine"."OrderId"
        FROM Products AS "prod"
        JOIN order_lines AS "OrderLine"
            ON "prod"."Id" = "OrderLine"."ProductItemNumber"
        """
    ])];

    public static TheoryData<SqlTestCase> SelfJoinData => [new SqlTestCase([
        """
        SELECT
            "original"."Id",
            "related"."Id"
        FROM "Products" AS "original"
        JOIN "Products" AS "related"
            ON "original"."CategoryId" = "related"."CategoryId"
        """
    ])];

    public static TheoryData<SqlTestCase> JoinWithConfigOverrideData => [new SqlTestCase([
        """
        SELECT
            "Archive_Products"."Id",
            "OrderLine"."OrderId"
        FROM "Archive_Products"
        JOIN "OrderLine"
            ON "Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
        """
    ])];
}