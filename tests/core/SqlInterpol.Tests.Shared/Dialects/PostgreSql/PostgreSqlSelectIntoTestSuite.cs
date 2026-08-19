using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlSelectIntoTestSuite : ISelectIntoTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> SelectIntoData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
        INTO #TempProducts
        FROM "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> SelectIntoParameterizedData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
        INTO #TempProducts
        FROM "dbo"."Products"
        """
    ])];
}