using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdWhereSubqueryTestSuiteBase : IWhereSubqueryTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

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
                FROM "Products" AS "p"
                WHERE "p"."Price" > 0
            )
            """
        ]
    )];
}