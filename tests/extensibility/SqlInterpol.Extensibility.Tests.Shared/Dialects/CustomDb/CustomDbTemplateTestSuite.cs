using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbTemplateTestSuite : ITemplateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> TemplateSelectData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT <<o1>>.<<Id>>, <<o1>>.<<CustomerId>>
            FROM <<dbo>>.<<Orders>> AS <<o1>>
            WHERE <<o1>>.<<CustomerId>> = !!100
            ORDER BY <<o1>>.<<Id>> DESC
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateBulkInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO <<dbo>>.<<Orders>> (<<Id>>)
            VALUES (!!100), (!!101)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE <<dbo>>.<<Orders>> SET <<CustomerId>> = !!100 WHERE <<Id>> = !!101;
            UPDATE <<dbo>>.<<Orders>> SET <<CustomerId>> = !!102 WHERE <<Id>> = !!103
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            DELETE FROM <<dbo>>.<<Orders>> WHERE <<Id>> = !!100;
            DELETE FROM <<dbo>>.<<Orders>> WHERE <<Id>> = !!101
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO <<dbo>>.<<Orders>>
            (<<Id>>, <<CustomerId>>)
            VALUES (!!100, !!101)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE <<dbo>>.<<Orders>>
            SET <<CustomerId>> = !!100
            WHERE <<Id>> = !!101
            """
        ]
    )];
}