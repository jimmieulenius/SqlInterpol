using System;
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbSqlBuilderTestSuite : ISqlBuilderTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> AppendData => [new SqlTestCase([
        "SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>"
    ])];

    public static TheoryData<SqlTestCase> AppendLineData => [new SqlTestCase([
        $"SELECT <<dbo>>.<<Products>>.<<Id>>{Environment.NewLine}FROM <<dbo>>.<<Products>>"
    ])];

    public static TheoryData<SqlTestCase> RawStringData => [new SqlTestCase([
        """
        SELECT
            <<dbo>>.<<Products>>.<<Id>>
        FROM <<dbo>>.<<Products>>
        """
    ])];
}