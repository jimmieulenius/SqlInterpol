using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqlServer;

public abstract partial class SqlServerPreprocessorExtensibilityTestSuiteBase : IPreprocessorExtensibilityTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqlServer(options);

    public static TheoryData<SqlTestCase> CustomRuleData => [new SqlTestCase([
        """
        SELECT 'Do not touch MAGIC_FUNC' AS LiteralString,
               REAL_FUNC(Id) AS ComputedId
        FROM Users
        """
    ])];
}