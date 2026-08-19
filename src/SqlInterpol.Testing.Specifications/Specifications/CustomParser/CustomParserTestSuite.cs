using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;
using SqlInterpol.Testing.Specifications.Pipeline;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ICustomParserTestSuite))]
public abstract partial class CustomParserTestSuite
{
    private static readonly SqlInterpolOptions _options = new() { Preprocessor = new CustomSqlPreprocessor() };

    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    protected static readonly List<int> ActiveIds = [10, 20, 30];

    [SqlTest(nameof(ICustomParserTestSuite.CustomParserData))]
    public void CustomParser(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder(_options);

        // Act
        testCase.Act(() => db.Append($$"""
            SELECT *
            FROM Users
            WHERE RoleId CUSTOM_IN {{ActiveIds}}
            """).Build()
        );

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}