using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbDeleteSubqueryTestSuite : IDeleteSubqueryTestSuite
{
    private static readonly object[] _expectedParameters = ["Cancelled"];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> Delete_WithSubqueryData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM <<OrderLine>>
                WHERE <<OrderLine>>.<<OrderId>> IN (
                    SELECT <<dbo>>.<<Orders>>.<<Id>>
                    FROM <<dbo>>.<<Orders>>
                    WHERE <<dbo>>.<<Orders>>.<<order_status>> = !!100
                )
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}