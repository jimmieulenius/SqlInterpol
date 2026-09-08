using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlOptionsTestSuite : IOptionsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> CustomParameterIndexStartData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = $50 AND Age = $51"],
        expectedParameters: [100, 200]
    )];

    public static TheoryData<SqlTestCase> EnumFormattingData => [new SqlTestCase(
        expectedSql: ["UPDATE \"dbo\".\"Users\" SET \"Status\" = $1"],
        expectedParameters: ["Active"]
    )];

    public static TheoryData<SqlTestCase> CrossDialectTranspilationWithCustomOptionsData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Products LIMIT $1 OFFSET $2"],
        expectedParameters: [10, 20]
    )];
}