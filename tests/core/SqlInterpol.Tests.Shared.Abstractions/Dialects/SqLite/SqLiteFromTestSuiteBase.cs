using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteFromTestSuiteBase : IFromTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> From_SingleEntityData => [new SqlTestCase([
        """
        SELECT *
        FROM "OrderLine"
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityWithSqlTableNameOnlyData => [new SqlTestCase([
        """
        SELECT *
        FROM "MyTable"
        """
    ])];

    public static TheoryData<SqlTestCase> From_Entity_WithSqlTableNameAndSchemaData => [new SqlTestCase([
        """
        SELECT *
        FROM "MyTable"
        """
    ])];
}