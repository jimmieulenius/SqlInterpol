using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlParametersTestSuite : IParametersTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> SingleParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = @p0 AND Name = @p1"],
        expectedParameters: [42, "Test"]
    )];

    public static TheoryData<SqlTestCase> NullParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = @p0"],
        expectedParameters: [null]
    )];

    public static TheoryData<SqlTestCase> CollectionParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id IN (@p0, @p1, @p2)"],
        expectedParameters: [1, 2, 3]
    )];

    public static TheoryData<SqlTestCase> ExplicitSqlArgData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = @p0"],
        expectedParameters: ["Alice"]
    )];
}