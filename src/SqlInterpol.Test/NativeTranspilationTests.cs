using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class NativeTranspilationTests
{
    [Theory]
    [MemberData(nameof(BooleanTranspilationData))]
    public void Boolean_Keywords_Transpile_To_Numeric_Where_Required(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Append($"""
            SELECT * FROM Users WHERE IsActive = TRUE AND IsDeleted = FALSE
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(BooleanBoundaryData))]
    public void Boolean_Keywords_Respect_Word_Boundaries(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Append($"""
            SELECT CONSTRUE, FALSEHOOD FROM TRUE_TABLE WHERE IsActive = TRUE
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(ConcatOperatorData))]
    public void Concat_Operator_Transpiles_To_Plus_Where_Required(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Append($"""
            SELECT FirstName || ' ' || LastName FROM Users
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(StringSafetyData))]
    public void Transpilation_Ignores_Keywords_Inside_String_Literals(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Append($"""
            SELECT 'The statement is TRUE || FALSE' FROM Users WHERE IsActive = TRUE
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> BooleanTranspilationData
    {
        get
        {
            var standardSql = "SELECT * FROM Users WHERE IsActive = TRUE AND IsDeleted = FALSE";
            var numericSql = "SELECT * FROM Users WHERE IsActive = 1 AND IsDeleted = 0";

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, [standardSql]),
                new SqlTestCase(SqlDialectKind.Firebird, [standardSql]),
                new SqlTestCase(SqlDialectKind.MySql, [standardSql]),
                new SqlTestCase(SqlDialectKind.PostgreSql, [standardSql]),
                new SqlTestCase(SqlDialectKind.SqLite, [standardSql]),
                
                // Transpiled
                new SqlTestCase(SqlDialectKind.Oracle, [numericSql]),
                new SqlTestCase(SqlDialectKind.SqlServer, [numericSql])
            ];
        }
    }

    public static TheoryData<SqlTestCase> BooleanBoundaryData
    {
        get
        {
            var standardSql = "SELECT CONSTRUE, FALSEHOOD FROM TRUE_TABLE WHERE IsActive = TRUE";
            var numericSql = "SELECT CONSTRUE, FALSEHOOD FROM TRUE_TABLE WHERE IsActive = 1";

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, [standardSql]),
                new SqlTestCase(SqlDialectKind.Firebird, [standardSql]),
                new SqlTestCase(SqlDialectKind.MySql, [standardSql]),
                new SqlTestCase(SqlDialectKind.PostgreSql, [standardSql]),
                new SqlTestCase(SqlDialectKind.SqLite, [standardSql]),
                
                // Transpiled
                new SqlTestCase(SqlDialectKind.Oracle, [numericSql]),
                new SqlTestCase(SqlDialectKind.SqlServer, [numericSql])
            ];
        }
    }

    public static TheoryData<SqlTestCase> ConcatOperatorData
    {
        get
        {
            var standardSql = "SELECT FirstName || ' ' || LastName FROM Users";
            var plusSql = "SELECT FirstName + ' ' + LastName FROM Users";

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, [standardSql]),
                new SqlTestCase(SqlDialectKind.Firebird, [standardSql]),
                new SqlTestCase(SqlDialectKind.MySql, [standardSql]),
                new SqlTestCase(SqlDialectKind.Oracle, [standardSql]),
                new SqlTestCase(SqlDialectKind.PostgreSql, [standardSql]),
                new SqlTestCase(SqlDialectKind.SqLite, [standardSql]),
                
                // Transpiled
                new SqlTestCase(SqlDialectKind.SqlServer, [plusSql])
            ];
        }
    }

    public static TheoryData<SqlTestCase> StringSafetyData
    {
        get
        {
            // All dialects should protect the contents of the string literal!
            var standardSql = "SELECT 'The statement is TRUE || FALSE' FROM Users WHERE IsActive = TRUE";
            var numericSql = "SELECT 'The statement is TRUE || FALSE' FROM Users WHERE IsActive = 1";

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, [standardSql]),
                new SqlTestCase(SqlDialectKind.Firebird, [standardSql]),
                new SqlTestCase(SqlDialectKind.MySql, [standardSql]),
                new SqlTestCase(SqlDialectKind.PostgreSql, [standardSql]),
                new SqlTestCase(SqlDialectKind.SqLite, [standardSql]),
                
                // Only the outside keyword is transpiled
                new SqlTestCase(SqlDialectKind.Oracle, [numericSql]),
                new SqlTestCase(SqlDialectKind.SqlServer, [numericSql])
            ];
        }
    }
}