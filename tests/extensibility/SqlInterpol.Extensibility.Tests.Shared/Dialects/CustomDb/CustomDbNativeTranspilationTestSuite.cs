using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbNativeTranspilationTestSuite : INativeTranspilationTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> BooleanTranspilationData => [new SqlTestCase([
        """
        SELECT * FROM Users WHERE IsActive = TRUE AND IsDeleted = FALSE
        """
    ])];

    public static TheoryData<SqlTestCase> BooleanBoundaryData => [new SqlTestCase([
        """
        SELECT CONSTRUE, FALSEHOOD FROM TRUE_TABLE WHERE IsActive = TRUE
        """
    ])];

    public static TheoryData<SqlTestCase> ConcatOperatorData => [new SqlTestCase([
        """
        SELECT FirstName || ' ' || LastName FROM Users
        """
    ])];

    public static TheoryData<SqlTestCase> StringSafetyData => [new SqlTestCase([
        """
        SELECT 'The statement is TRUE || FALSE' FROM Users WHERE IsActive = TRUE
        """
    ])];
}