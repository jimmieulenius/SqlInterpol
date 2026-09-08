using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleOptionsTestSuite : IOptionsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> CustomParameterIndexStartData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = :50 AND Age = :51"],
        expectedParameters: [100, 200]
    )];

    public static TheoryData<SqlTestCase> EnumFormattingData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Users\" SET \"Status\" = :0"],
        expectedParameters: ["Active"]
    )];

    public static TheoryData<SqlTestCase> CrossDialectTranspilationWithCustomOptionsData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Products OFFSET :1 ROWS FETCH NEXT :0 ROWS ONLY"],
        expectedParameters: [10, 20]
    )];
}