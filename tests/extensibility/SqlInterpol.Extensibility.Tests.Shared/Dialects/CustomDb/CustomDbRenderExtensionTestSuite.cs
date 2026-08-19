using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbRenderExtensionTestSuite : IRenderExtensionTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> AsDeclarationData => [new SqlTestCase(["SELECT * FROM <<dbo>>.<<Products>> AS <<prod>>"])];
    public static TheoryData<SqlTestCase> AsAliasData => [new SqlTestCase(["SELECT <<prod>>.* FROM dbo.Products AS <<prod>>"])];
    public static TheoryData<SqlTestCase> AsBaseData => [new SqlTestCase(["TRUNCATE TABLE <<dbo>>.<<Products>>"])];
    public static TheoryData<SqlTestCase> AsColumnData => [new SqlTestCase(["SELECT <<PROD_NAME>> FROM <<dbo>>.<<Products>> AS <<prod>>"])];

    public static TheoryData<SqlTestCase> CombinedData => [new SqlTestCase([
        """
        SELECT 
            <<Id>>, 
            <<prod>>.<<PROD_NAME>>
        FROM <<dbo>>.<<Products>> AS <<prod>>
        INNER JOIN <<dbo>>.<<Products>> AS <<backup_prod>> ON <<backup_prod>>.Id = <<prod>>.Id
        """
    ])];
}