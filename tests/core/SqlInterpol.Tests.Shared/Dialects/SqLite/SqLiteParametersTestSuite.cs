using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteParametersTestSuite : IParametersTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> SingleParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = @p1 AND Name = @p2"],
        expectedParameters: [42, "Test"]
    )];

    public static TheoryData<SqlTestCase> NullParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = @p1"],
        expectedParameters: [null]
    )];

    public static TheoryData<SqlTestCase> CollectionParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id IN (@p1, @p2, @p3)"],
        expectedParameters: [1, 2, 3]
    )];

    public static TheoryData<SqlTestCase> ExplicitSqlArgData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = @p1"],
        expectedParameters: ["Alice"]
    )];
}