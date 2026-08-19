using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlSelectTestSuite : ISelectTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> SelectExpansionData => [new SqlTestCase([
        "SELECT \"p1\".\"Id\", \"p1\".\"PROD_NAME\"\nFROM \"dbo\".\"Products\" AS \"p1\""
    ])];

    public static TheoryData<SqlTestCase> SingleColumnData => [new SqlTestCase([
        "SELECT\n    \"dbo\".\"Products\".\"Id\"\nFROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> MultipleColumnsData => [new SqlTestCase([
        "SELECT\n    \"dbo\".\"Products\".\"Id\",\n    \"dbo\".\"Products\".\"CategoryId\"\nFROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> SqlFunctionData => [new SqlTestCase([
        "SELECT\n    COUNT(\"dbo\".\"Products\".\"Id\")\nFROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> LiteralParameterData => [new SqlTestCase([
        "SELECT\n    $1\nFROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> CustomColumnAttributeData => [new SqlTestCase([
        "SELECT\n    \"dbo\".\"Products\".\"PROD_NAME\"\nFROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> SelectDistinctVerticalLayoutData => [new SqlTestCase([
        "SELECT DISTINCT\n    \"p1\".\"Id\",\n    \"p1\".\"PROD_NAME\"\nFROM \"dbo\".\"Products\" AS \"p1\""
    ])];

    public static TheoryData<SqlTestCase> TopKeywordData => [new SqlTestCase([
        "SELECT TOP 10 \"dbo\".\"Products\".\"Id\"\nFROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> SelectComplexData => [new SqlTestCase([
        "SELECT \"p\".\"Category\", \"p\".\"Id\", \"p\".\"Name\", \"p\".\"Status\"\nFROM \"tbl_complex_products\" AS \"p\""
    ])];
}