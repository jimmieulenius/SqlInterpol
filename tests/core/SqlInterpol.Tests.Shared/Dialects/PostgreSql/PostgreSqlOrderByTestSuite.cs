using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlOrderByTestSuite : IOrderByTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> OrderByExpressionData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Orders"
        ORDER BY "dbo"."Orders"."created_at" DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByCombinerData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Orders"
        ORDER BY "dbo"."Orders"."Total", "dbo"."Orders"."Id" DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByEnumerableData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Orders"
        ORDER BY "dbo"."Orders"."Total" ASC, "dbo"."Orders"."Id" DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByRawData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Orders"
        ORDER BY Total DESC
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByMixedRawData => [new SqlTestCase([
        """
        SELECT *
        FROM "dbo"."Orders"
        ORDER BY "dbo"."Orders"."created_at" ASC, (Total * 0.9) DESC
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