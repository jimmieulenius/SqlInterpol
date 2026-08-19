using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IPreprocessorExtensibilityTestSuite))]
public abstract partial class PreprocessorExtensibilityTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IPreprocessorExtensibilityTestSuite.CustomRuleData))]
    public void Preprocessor_ExecutesCustomRules_AndSafelyTranspilesKeywords(SqlTestCase testCase)
    {
        // Arrange - Inject our custom rule into the pipeline options
        var options = new SqlInterpolOptions();
        options.PreprocessorRules.Add(new SafeKeywordPreprocessorRule());
        
        var db = CreateBuilder(options);

        // Act
        testCase.Act(() => 
        {
            return db.Append($"""
                SELECT 'Do not touch MAGIC_FUNC' AS LiteralString,
                       MAGIC_FUNC(Id) AS ComputedId
                FROM Users
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}