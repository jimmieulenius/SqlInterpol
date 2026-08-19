using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbJoinTestSuite : IJoinTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> JoinTwoEntitiesData => [new SqlTestCase([
        """
        SELECT
            <<dbo>>.<<Products>>.<<Id>>,
            <<OrderLine>>.<<OrderId>>
        FROM <<dbo>>.<<Products>>
        JOIN <<OrderLine>>
            ON <<dbo>>.<<Products>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
        """
    ])];
}