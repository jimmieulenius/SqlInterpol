using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlOptionsTestSuite : IOptionsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> CustomParameterIndexStartData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = @p50 AND Age = @p51"],
        expectedParameters: [100, 200]
    )];

    public static TheoryData<SqlTestCase> EnumFormattingData => [new SqlTestCase(
        expectedSql: ["UPDATE `dbo`.`Users` SET `Status` = @p0"],
        expectedParameters: ["Active"]
    )];

    public static TheoryData<SqlTestCase> CrossDialectTranspilationWithCustomOptionsData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Products LIMIT @p0 OFFSET @p1"],
        expectedParameters: [10, 20]
    )];
}