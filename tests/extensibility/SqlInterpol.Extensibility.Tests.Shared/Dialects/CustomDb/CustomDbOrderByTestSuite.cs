using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbOrderByTestSuite : IOrderByTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> OrderByExpressionData => [new SqlTestCase([
        """
        SELECT *
        FROM <<dbo>>.<<Orders>>
        ORDER BY <<dbo>>.<<Orders>>.<<created_at>> DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByCombinerData => [new SqlTestCase([
        """
        SELECT *
        FROM <<dbo>>.<<Orders>>
        ORDER BY <<dbo>>.<<Orders>>.<<Total>>, <<dbo>>.<<Orders>>.<<Id>> DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByEnumerableData => [new SqlTestCase([
        """
        SELECT *
        FROM <<dbo>>.<<Orders>>
        ORDER BY <<dbo>>.<<Orders>>.<<Total>> ASC, <<dbo>>.<<Orders>>.<<Id>> DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByRawData => [new SqlTestCase([
        """
        SELECT *
        FROM <<dbo>>.<<Orders>>
        ORDER BY Total DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByMixedRawData => [new SqlTestCase([
        """
        SELECT *
        FROM <<dbo>>.<<Orders>>
        ORDER BY <<dbo>>.<<Orders>>.<<created_at>> ASC, (Total * 0.9) DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByErrorData => [
        new SqlTestCase(
            expectedExceptionType: typeof(ArgumentException),
            expectedExceptionMessage: $"Property 'FakeColumn' not found on 'Product'."
        ),
        new SqlTestCase(
            expectedExceptionType: typeof(ArgumentException),
            expectedExceptionMessage: $"Property 'UnmappedProperty' not found on 'OrderTestModel'."
        )
    ];
}