using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteWhereSubqueryTestSuiteBase : IWhereSubqueryTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

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