using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdNativeTranspilationTestSuite : INativeTranspilationTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

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