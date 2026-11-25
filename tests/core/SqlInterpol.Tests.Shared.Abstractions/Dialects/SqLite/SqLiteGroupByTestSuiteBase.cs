using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteGroupByTestSuiteBase : IGroupByTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> GroupByCombinerData => [new SqlTestCase([
        """
        SELECT CategoryId, order_status, COUNT(*)
        FROM "Orders"
        GROUP BY "Orders"."CategoryId", "Orders"."order_status"
        """
    ])];

    public static TheoryData<SqlTestCase> GroupByWithSqlRawData => [new SqlTestCase([
        """
        SELECT YEAR(created_at), COUNT(*)
        FROM "Orders"
        GROUP BY YEAR(created_at)
        """
    ])];

    public static TheoryData<SqlTestCase> GroupByMixingTypedAndRawData => [new SqlTestCase([
        """
        SELECT order_status, YEAR(created_at), COUNT(*)
        FROM "Orders"
        GROUP BY "Orders"."order_status", YEAR(created_at)
        """
    ])];
}