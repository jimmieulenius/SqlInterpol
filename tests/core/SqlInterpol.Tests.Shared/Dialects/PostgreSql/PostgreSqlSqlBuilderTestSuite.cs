using System;
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlSqlBuilderTestSuite : ISqlBuilderTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> AppendData => [new SqlTestCase([
        "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> AppendLineData => [new SqlTestCase([
        $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
    ])];

    public static TheoryData<SqlTestCase> RawStringData => [new SqlTestCase([
        """
        SELECT
            "dbo"."Products"."Id"
        FROM "dbo"."Products"
        """
    ])];
}