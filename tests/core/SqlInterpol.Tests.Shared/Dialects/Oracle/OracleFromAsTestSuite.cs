using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleFromAsTestSuite : IFromAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> From_EntityManualAliasData => [new SqlTestCase([
        """
        SELECT
            "p"."Id"
        FROM "dbo"."Products" "p"
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntitySqlTableAttributeData => [new SqlTestCase([
        """
        SELECT
            "prod"."Id"
        FROM "dbo"."Products" "prod"
        """
    ])];

    public static TheoryData<SqlTestCase> From_LiteralTableAsEntityWithoutAttributeData => [new SqlTestCase([
        """
        SELECT
            "OrderLine"."OrderId"
        FROM ORDER_LINES "OrderLine"
        """
    ])];

    public static TheoryData<SqlTestCase> From_LiteralTableAsExplicitAliasedEntityData => [new SqlTestCase([
        """
        SELECT
            "prod"."Id"
        FROM products "prod"
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityAsEntityWithSchemaData => [new SqlTestCase([
        """
        SELECT
            "Product"."Id"
        FROM "dbo"."Products" "Product"
        """
    ])];

    public static TheoryData<SqlTestCase> FromAsEntityAsItsOwnAliasInceptionData => [new SqlTestCase([
        """
        SELECT "Product"."CategoryId", "Product"."Id", "Product"."IsActive", "Product"."PROD_NAME", "Product"."Price"
        FROM "dbo"."Products" "Product"
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityAutoAliasingData => [new SqlTestCase([
        """
        SELECT
            "prod"."Id"
        FROM "dbo"."Products" "prod"
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityAutoAliasingManualOverrideData => [new SqlTestCase([
        """
        SELECT
            "p"."Id"
        FROM "dbo"."Products" "p"
        """
    ])];

    public static TheoryData<SqlTestCase> From_AutoAliasingInceptionData => [new SqlTestCase([
        """
        SELECT "myProd"."CategoryId", "myProd"."Id", "myProd"."IsActive", "myProd"."PROD_NAME", "myProd"."Price"
        FROM "dbo"."Products" "myProd"
        """
    ])];
}