using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public abstract partial class SqlServerFromTestSuiteBase : IFromTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> From_SingleEntityData => [new SqlTestCase([
        """
        SELECT *
        FROM [OrderLine]
        """
    ])];

    public static TheoryData<SqlTestCase> From_EntityWithSqlTableNameOnlyData => [new SqlTestCase([
        """
        SELECT *
        FROM [MyTable]
        """
    ])];

    public static TheoryData<SqlTestCase> From_Entity_WithSqlTableNameAndSchemaData => [new SqlTestCase([
        """
        SELECT *
        FROM [MySchema].[MyTable]
        """
    ])];
}