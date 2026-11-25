using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLitePreprocessorExtensibilityTestSuiteBase : IPreprocessorExtensibilityTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> CustomRuleData => [new SqlTestCase([
        """
        SELECT 'Do not touch MAGIC_FUNC' AS LiteralString,
               REAL_FUNC(Id) AS ComputedId
        FROM Users
        """
    ])];
}