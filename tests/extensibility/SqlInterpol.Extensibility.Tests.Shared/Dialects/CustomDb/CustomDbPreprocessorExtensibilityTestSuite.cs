using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbPreprocessorExtensibilityTestSuite : IPreprocessorExtensibilityTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> CustomRuleData => [new SqlTestCase([
        """
        SELECT 'Do not touch MAGIC_FUNC' AS LiteralString,
               REAL_FUNC(Id) AS ComputedId
        FROM Users
        """
    ])];
}