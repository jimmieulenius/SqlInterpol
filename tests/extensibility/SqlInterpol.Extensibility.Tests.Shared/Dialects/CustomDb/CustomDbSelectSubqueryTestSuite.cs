using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbSelectSubqueryTestSuite : ISelectSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> SelectSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                <<prod>>.<<Id>>,
                (
                    SELECT
                        <<Category>>.<<Name>>
                    FROM <<Category>>
                    WHERE <<Category>>.<<Id>> = <<prod>>.<<CategoryId>> AND <<Category>>.<<IsActive>> = !!100
                ) AS <<CategoryName>>
            FROM <<dbo>>.<<Products>> AS <<prod>>
            WHERE <<prod>>.<<Price>> > !!101
            """,
            """
            SELECT 
                <<second_prod>>.<<Id>>,
                (
                    SELECT
                        <<Category>>.<<Name>>
                    FROM <<Category>>
                    WHERE <<Category>>.<<Id>> = <<second_prod>>.<<CategoryId>> AND <<Category>>.<<IsActive>> = !!100
                ) AS <<CategoryName>>
            FROM <<dbo>>.<<Products>> AS <<second_prod>>
            WHERE <<second_prod>>.<<Price>> > !!101
            """
        ],
        expectedParameters: [100, 101]
    )];
}