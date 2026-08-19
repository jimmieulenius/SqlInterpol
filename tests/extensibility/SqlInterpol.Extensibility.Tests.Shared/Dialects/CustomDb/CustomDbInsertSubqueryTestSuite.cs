using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbInsertSubqueryTestSuite : IInsertSubqueryTestSuite
{
    private static readonly object[] _expectedParameters = [100];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> InsertSelectData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO <<dbo>>.<<Orders>> 
            (<<Id>>, <<Total>>)
            SELECT <<OrderLine>>.<<OrderId>>, <<OrderLine>>.<<Quantity>>
            FROM <<OrderLine>>
            WHERE <<OrderLine>>.<<OrderId>> = !!100
            """
        ],
        expectedParameters: _expectedParameters
    )];
}