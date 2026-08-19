using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(INativeTranspilationTestSuite))]
public abstract partial class NativeTranspilationTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(INativeTranspilationTestSuite.BooleanTranspilationData))]
    public void Boolean_Keywords_Transpile_To_Numeric_Where_Required(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            return db.Append($"""
                SELECT * FROM Users WHERE IsActive = TRUE AND IsDeleted = FALSE
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(INativeTranspilationTestSuite.BooleanBoundaryData))]
    public void Boolean_Keywords_Respect_Word_Boundaries(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            return db.Append($"""
                SELECT CONSTRUE, FALSEHOOD FROM TRUE_TABLE WHERE IsActive = TRUE
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(INativeTranspilationTestSuite.ConcatOperatorData))]
    public void Concat_Operator_Transpiles_To_Plus_Where_Required(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            return db.Append($"""
                SELECT FirstName || ' ' || LastName FROM Users
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(INativeTranspilationTestSuite.StringSafetyData))]
    public void Transpilation_Ignores_Keywords_Inside_String_Literals(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            return db.Append($"""
                SELECT 'The statement is TRUE || FALSE' FROM Users WHERE IsActive = TRUE
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}