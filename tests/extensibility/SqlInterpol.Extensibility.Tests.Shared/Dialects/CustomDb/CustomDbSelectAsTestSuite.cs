using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbSelectAsTestSuite : ISelectAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> ProjectionAsLiteralData => [new SqlTestCase([
        """
        SELECT
            <<dbo>>.<<Products>>.<<Id>> AS <<ProductId>>
        FROM <<dbo>>.<<Products>>
        """
    ])];

    public static TheoryData<SqlTestCase> RawColumnAsProjectionData => [new SqlTestCase([
        """
        SELECT
            <<dbo>>.<<Products>>.<<Id>> AS <<ProductId>>
        FROM <<dbo>>.<<Products>>
        """
    ])];

    public static TheoryData<SqlTestCase> ProjectionAsProjectionWithAttributeData => [new SqlTestCase([
        """
        SELECT
            <<dbo>>.<<Products>>.<<PROD_NAME>> AS <<Name>>
        FROM <<dbo>>.<<Products>>
        """
    ])];
}