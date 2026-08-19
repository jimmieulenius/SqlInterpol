using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleJoinAsTestSuite : IJoinAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> JoinWithLiteralAliasesData => [new SqlTestCase([
        """
        SELECT
            "p"."Id",
            "ol"."OrderId"
        FROM "dbo"."Products" "p"
        JOIN "OrderLine" "ol"
            ON "p"."Id" = "ol"."ProductItemNumber"
        """
    ])];

    public static TheoryData<SqlTestCase> JoinWithExplicitApiAliasesData => [new SqlTestCase([
        """
        SELECT
            "prod"."Id",
            "OrderLine"."OrderId"
        FROM dbo.Products "prod"
        JOIN order_lines "OrderLine"
            ON "prod"."Id" = "OrderLine"."ProductItemNumber"
        """
    ])];

    public static TheoryData<SqlTestCase> SelfJoinData => [new SqlTestCase([
        """
        SELECT
            "original"."Id",
            "related"."Id"
        FROM "dbo"."Products" "original"
        JOIN "dbo"."Products" "related"
            ON "original"."CategoryId" = "related"."CategoryId"
        """
    ])];

    public static TheoryData<SqlTestCase> JoinWithConfigOverrideData => [new SqlTestCase([
        """
        SELECT
            "history"."Archive_Products"."Id",
            "OrderLine"."OrderId"
        FROM "history"."Archive_Products"
        JOIN "OrderLine"
            ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
        """
    ])];
}