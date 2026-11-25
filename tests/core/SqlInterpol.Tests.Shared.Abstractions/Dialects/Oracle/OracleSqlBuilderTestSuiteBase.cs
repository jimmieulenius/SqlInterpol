using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleSqlBuilderTestSuiteBase : ISqlBuilderTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> AppendData => [new SqlTestCase([
        "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> AppendLineData => [new SqlTestCase([
        $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> RawStringData => [new SqlTestCase([
        """
        SELECT
            "dbo"."Products"."Id"
        FROM "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> FluentMappingData => [new SqlTestCase([
        """
        SELECT "tbl_orders"."Id", "tbl_orders"."order_total"
        FROM "tbl_orders"
        """
    ])];
}