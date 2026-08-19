using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbGroupByAsTestSuite : IGroupByAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> GroupByWithExplicitAliasData => [new SqlTestCase([
        """
        SELECT <<prod>>.<<PROD_NAME>>, <<prod>>.<<IsActive>>, COUNT(*)
        FROM <<dbo>>.<<Products>> AS <<prod>>
        GROUP BY <<prod>>.<<PROD_NAME>>, <<prod>>.<<IsActive>>
        """
    ])];
}