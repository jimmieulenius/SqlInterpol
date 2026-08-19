using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleRenderExtensionTestSuite : IRenderExtensionTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> AsDeclarationData => [new SqlTestCase(["SELECT * FROM \"dbo\".\"Products\" \"prod\""])];
    public static TheoryData<SqlTestCase> AsAliasData => [new SqlTestCase(["SELECT \"prod\".* FROM dbo.Products \"prod\""])];
    public static TheoryData<SqlTestCase> AsBaseData => [new SqlTestCase(["TRUNCATE TABLE \"dbo\".\"Products\""])];
    public static TheoryData<SqlTestCase> AsColumnData => [new SqlTestCase(["SELECT \"PROD_NAME\" FROM \"dbo\".\"Products\" \"prod\""])];

    public static TheoryData<SqlTestCase> CombinedData => [new SqlTestCase([
        """
        SELECT 
            "Id", 
            "prod"."PROD_NAME"
        FROM "dbo"."Products" "prod"
        INNER JOIN "dbo"."Products" "backup_prod" ON "backup_prod".Id = "prod".Id
        """
    ])];
}