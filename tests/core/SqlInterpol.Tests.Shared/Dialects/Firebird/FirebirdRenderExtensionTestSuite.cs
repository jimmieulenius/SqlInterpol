using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdRenderExtensionTestSuite : IRenderExtensionTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> AsDeclarationData => [new SqlTestCase(["SELECT * FROM \"dbo\".\"Products\" AS \"prod\""])];
    public static TheoryData<SqlTestCase> AsAliasData => [new SqlTestCase(["SELECT \"prod\".* FROM dbo.Products AS \"prod\""])];
    public static TheoryData<SqlTestCase> AsBaseData => [new SqlTestCase(["TRUNCATE TABLE \"dbo\".\"Products\""])];
    public static TheoryData<SqlTestCase> AsColumnData => [new SqlTestCase(["SELECT \"PROD_NAME\" FROM \"dbo\".\"Products\" AS \"prod\""])];

    public static TheoryData<SqlTestCase> CombinedData => [new SqlTestCase([
        """
        SELECT 
            "Id", 
            "prod"."PROD_NAME"
        FROM "dbo"."Products" AS "prod"
        INNER JOIN "dbo"."Products" AS "backup_prod" ON "backup_prod".Id = "prod".Id
        """
    ])];
}