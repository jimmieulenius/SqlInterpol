using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleWhereSubqueryTestSuite : IWhereSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> WhereInSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "c"."Name"
            FROM "Category" "c"
            WHERE "c"."Id" IN
            (
                SELECT 
                    "p"."CategoryId"
                FROM "dbo"."Products" "p"
                WHERE "p"."Price" > 0
            )
            """
        ]
    )];
}