using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdGroupByTestSuite : IGroupByTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> GroupByCombinerData => [new SqlTestCase([
        """
        SELECT CategoryId, order_status, COUNT(*)
        FROM "dbo"."Orders"
        GROUP BY "dbo"."Orders"."CategoryId", "dbo"."Orders"."order_status"
        """
    ])];

    public static TheoryData<SqlTestCase> GroupByWithSqlRawData => [new SqlTestCase([
        """
        SELECT YEAR(created_at), COUNT(*)
        FROM "dbo"."Orders"
        GROUP BY YEAR(created_at)
        """
    ])];

    public static TheoryData<SqlTestCase> GroupByMixingTypedAndRawData => [new SqlTestCase([
        """
        SELECT order_status, YEAR(created_at), COUNT(*)
        FROM "dbo"."Orders"
        GROUP BY "dbo"."Orders"."order_status", YEAR(created_at)
        """
    ])];
}