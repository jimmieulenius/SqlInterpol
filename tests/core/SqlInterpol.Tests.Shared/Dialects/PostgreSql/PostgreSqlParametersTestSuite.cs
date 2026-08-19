using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlParametersTestSuite : IParametersTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> SingleParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = $1 AND Name = $2"],
        expectedParameters: [42, "Test"]
    )];

    public static TheoryData<SqlTestCase> NullParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = $1"],
        expectedParameters: [null]
    )];

    public static TheoryData<SqlTestCase> CollectionParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id IN ($1, $2, $3)"],
        expectedParameters: [1, 2, 3]
    )];

    public static TheoryData<SqlTestCase> ExplicitSqlArgData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = $1"],
        expectedParameters: ["Alice"]
    )];
}