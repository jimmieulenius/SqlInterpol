using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdSelectTestSuiteBase : ISelectTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> SelectExpansionData => [new SqlTestCase([
        "SELECT \"p1\".\"Id\", \"p1\".\"PROD_NAME\"\nFROM \"Products\" AS \"p1\""
    ])];

    public static TheoryData<SqlTestCase> SingleColumnData => [new SqlTestCase([
        "SELECT\n    \"Products\".\"Id\"\nFROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> MultipleColumnsData => [new SqlTestCase([
        "SELECT\n    \"Products\".\"Id\",\n    \"Products\".\"CategoryId\"\nFROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> SqlFunctionData => [new SqlTestCase([
        "SELECT\n    COUNT(\"Products\".\"Id\")\nFROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> LiteralParameterData => [new SqlTestCase([
        "SELECT\n    @p0\nFROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> CustomColumnAttributeData => [new SqlTestCase([
        "SELECT\n    \"Products\".\"PROD_NAME\"\nFROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> SelectDistinctVerticalLayoutData => [new SqlTestCase([
        "SELECT DISTINCT\n    \"p1\".\"Id\",\n    \"p1\".\"PROD_NAME\"\nFROM \"Products\" AS \"p1\""
    ])];

    public static TheoryData<SqlTestCase> TopKeywordData => [new SqlTestCase([
        "SELECT TOP 10 \"Products\".\"Id\"\nFROM \"Products\""
    ])];

    public static TheoryData<SqlTestCase> SelectComplexData => [new SqlTestCase([
        "SELECT \"p\".\"Category\", \"p\".\"Id\", \"p\".\"Name\", \"p\".\"Status\"\nFROM \"tbl_complex_products\" AS \"p\""
    ])];
}