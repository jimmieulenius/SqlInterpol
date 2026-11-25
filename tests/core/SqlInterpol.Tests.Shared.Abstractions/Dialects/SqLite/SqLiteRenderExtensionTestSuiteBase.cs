using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteRenderExtensionTestSuiteBase : IRenderExtensionTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> AsDeclarationData => [new SqlTestCase(["SELECT * FROM \"Products\" AS \"prod\""])];
    public static TheoryData<SqlTestCase> AsAliasData => [new SqlTestCase(["SELECT \"prod\".* FROM Products AS \"prod\""])];
    public static TheoryData<SqlTestCase> AsBaseData => [new SqlTestCase(["TRUNCATE TABLE \"Products\""])];
    public static TheoryData<SqlTestCase> AsColumnData => [new SqlTestCase(["SELECT \"PROD_NAME\" FROM \"Products\" AS \"prod\""])];

    public static TheoryData<SqlTestCase> CombinedData => [new SqlTestCase([
        """
        SELECT 
            "Id", 
            "prod"."PROD_NAME"
        FROM "Products" AS "prod"
        INNER JOIN "Products" AS "backup_prod" ON "backup_prod".Id = "prod".Id
        """
    ])];
}