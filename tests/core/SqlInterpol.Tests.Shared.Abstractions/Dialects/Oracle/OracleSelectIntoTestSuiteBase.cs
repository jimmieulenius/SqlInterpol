using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleSelectIntoTestSuiteBase : ISelectIntoTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> SelectIntoData => [new SqlTestCase([
        """
        CREATE TABLE "#TempProducts" AS
        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
        FROM "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> SelectIntoParameterizedData => [new SqlTestCase([
        """
        CREATE TABLE #TempProducts AS
        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
        FROM "dbo"."Products"
        """
    ])];
}