using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteSqlBuilderTestSuiteBase : ISqlBuilderTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> AppendData => [new SqlTestCase([
        "SELECT \"Products\".\"Id\" FROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> AppendLineData => [new SqlTestCase([
        $"SELECT \"Products\".\"Id\"{Environment.NewLine}FROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> RawStringData => [new SqlTestCase([
        """
        SELECT
            "Products"."Id"
        FROM "Products"
        """
    ])];

    public static TheoryData<SqlTestCase> FluentMappingData => [new SqlTestCase([
        """
        SELECT "tbl_orders"."Id", "tbl_orders"."order_total"
        FROM "tbl_orders"
        """
    ])];
}