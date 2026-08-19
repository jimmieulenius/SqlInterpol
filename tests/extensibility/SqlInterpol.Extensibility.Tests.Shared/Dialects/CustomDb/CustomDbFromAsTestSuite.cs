using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbFromAsTestSuite : IFromAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> From_EntityManualAliasData => [new SqlTestCase([
        """
        SELECT
            <<p>>.<<Id>>
        FROM <<dbo>>.<<Products>> AS <<p>>
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntitySqlTableAttributeData => [new SqlTestCase([
        """
        SELECT
            <<prod>>.<<Id>>
        FROM <<dbo>>.<<Products>> AS <<prod>>
        """
    ])];

    public static TheoryData<SqlTestCase> From_LiteralTableAsEntityWithoutAttributeData => [new SqlTestCase([
        """
        SELECT
            <<OrderLine>>.<<OrderId>>
        FROM ORDER_LINES AS <<OrderLine>>
        """
    ])];

    public static TheoryData<SqlTestCase> From_LiteralTableAsExplicitAliasedEntityData => [new SqlTestCase([
        """
        SELECT
            <<prod>>.<<Id>>
        FROM products AS <<prod>>
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityAsEntityWithSchemaData => [new SqlTestCase([
        """
        SELECT
            <<Product>>.<<Id>>
        FROM <<dbo>>.<<Products>> AS <<Product>>
        """
    ])];

    public static TheoryData<SqlTestCase> FromAsEntityAsItsOwnAliasInceptionData => [new SqlTestCase([
        """
        SELECT <<Product>>.<<CategoryId>>, <<Product>>.<<Id>>, <<Product>>.<<IsActive>>, <<Product>>.<<PROD_NAME>>, <<Product>>.<<Price>>
        FROM <<dbo>>.<<Products>> AS <<Product>>
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityAutoAliasingData => [new SqlTestCase([
        """
        SELECT
            <<prod>>.<<Id>>
        FROM <<dbo>>.<<Products>> AS <<prod>>
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityAutoAliasingManualOverrideData => [new SqlTestCase([
        """
        SELECT
            <<p>>.<<Id>>
        FROM <<dbo>>.<<Products>> AS <<p>>
        """
    ])];

    public static TheoryData<SqlTestCase> From_AutoAliasingInceptionData => [new SqlTestCase([
        """
        SELECT <<myProd>>.<<CategoryId>>, <<myProd>>.<<Id>>, <<myProd>>.<<IsActive>>, <<myProd>>.<<PROD_NAME>>, <<myProd>>.<<Price>>
        FROM <<dbo>>.<<Products>> AS <<myProd>>
        """
    ])];
}