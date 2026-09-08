using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbOptionsTestSuite : IOptionsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> CustomParameterIndexStartData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Users WHERE Id = !!50 AND Age = !!51"],
        expectedParameters: [100, 200]
    )];

    public static TheoryData<SqlTestCase> EnumFormattingData => [new SqlTestCase(
        expectedSql: ["UPDATE <<dbo>>.<<Users>> SET <<Status>> = !!100"], // Inherits the 0-index because we overrode the options in the test!
        expectedParameters: ["Active"]
    )];

    public static TheoryData<SqlTestCase> CrossDialectTranspilationWithCustomOptionsData => [new SqlTestCase(
        expectedSql: ["SELECT * FROM Products LIMIT !!100 OFFSET !!101"],
        expectedParameters: [10, 20]
    )];
}