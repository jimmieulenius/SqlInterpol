using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbGroupByTestSuite : IGroupByTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> GroupByCombinerData => [new SqlTestCase([
        """
        SELECT CategoryId, order_status, COUNT(*)
        FROM <<dbo>>.<<Orders>>
        GROUP BY <<dbo>>.<<Orders>>.<<CategoryId>>, <<dbo>>.<<Orders>>.<<order_status>>
        """
    ])];

    public static TheoryData<SqlTestCase> GroupByWithSqlRawData => [new SqlTestCase([
        """
        SELECT YEAR(created_at), COUNT(*)
        FROM <<dbo>>.<<Orders>>
        GROUP BY YEAR(created_at)
        """
    ])];

    public static TheoryData<SqlTestCase> GroupByMixingTypedAndRawData => [new SqlTestCase([
        """
        SELECT order_status, YEAR(created_at), COUNT(*)
        FROM <<dbo>>.<<Orders>>
        GROUP BY <<dbo>>.<<Orders>>.<<order_status>>, YEAR(created_at)
        """
    ])];
}