using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdWhereSubqueryTestSuite : IWhereSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> WhereInSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT 
                "c"."Name"
            FROM "Category" AS "c"
            WHERE "c"."Id" IN
            (
                SELECT 
                    "p"."CategoryId"
                FROM "dbo"."Products" AS "p"
                WHERE "p"."Price" > 0
            )
            """
        ]
    )];
}