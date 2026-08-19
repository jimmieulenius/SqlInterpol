using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleParametersTestSuite : IParametersTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> SingleParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = :0 AND Name = :1"],
        expectedParameters: [42, "Test"]
    )];

    public static TheoryData<SqlTestCase> NullParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = :0"],
        expectedParameters: [null]
    )];

    public static TheoryData<SqlTestCase> CollectionParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id IN (:0, :1, :2)"],
        expectedParameters: [1, 2, 3]
    )];

    public static TheoryData<SqlTestCase> ExplicitSqlArgData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = :0"],
        expectedParameters: ["Alice"]
    )];
}