using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbParametersTestSuite : IParametersTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    // CustomDb's default options define ParameterIndexStart = 100
    public static TheoryData<SqlTestCase> SingleParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = !!100 AND Name = !!101"],
        expectedParameters: [42, "Test"]
    )];

    public static TheoryData<SqlTestCase> NullParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = !!100"],
        expectedParameters: [null]
    )];

    public static TheoryData<SqlTestCase> CollectionParameterData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id IN (!!100, !!101, !!102)"],
        expectedParameters: [1, 2, 3]
    )];

    public static TheoryData<SqlTestCase> ExplicitSqlArgData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Name = !!100"],
        expectedParameters: ["Alice"]
    )];
}