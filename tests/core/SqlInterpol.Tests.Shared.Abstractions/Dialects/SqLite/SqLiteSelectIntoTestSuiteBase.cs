using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteSelectIntoTestSuiteBase : ISelectIntoTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> SelectIntoData => [new SqlTestCase([
        """
        CREATE TABLE "#TempProducts" AS
        SELECT "Products"."Id", "Products"."PROD_NAME"
        FROM "Products"
        """
    ])];

    public static TheoryData<SqlTestCase> SelectIntoParameterizedData => [new SqlTestCase([
        """
        CREATE TABLE #TempProducts AS
        SELECT "Products"."Id", "Products"."PROD_NAME"
        FROM "Products"
        """
    ])];
}