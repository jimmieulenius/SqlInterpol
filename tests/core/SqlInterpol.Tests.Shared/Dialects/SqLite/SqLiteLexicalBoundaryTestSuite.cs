using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteLexicalBoundaryTestSuite : ILexicalBoundaryTestSuite
{
    private static readonly object[] _parameterizedExpectedParams = ["O'Connor"];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> EscapedQuotesInLiteralData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id"
        FROM "dbo"."Products"
        WHERE "dbo"."Products"."PROD_NAME" = 'O''Connor' AND "dbo"."Products"."CategoryId" = 1
        """
    ])];

    public static TheoryData<SqlTestCase> ParameterizedQuotesData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."PROD_NAME" = @p1
            """
        ],
        expectedParameters: _parameterizedExpectedParams
    )];

    public static TheoryData<SqlTestCase> MultiLineCommentWithQuotesData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id"
        /* This is a multi-line comment.
           It has 'single quotes' and "double quotes".
           The parser should completely ignore them.
        */
        FROM "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> SingleLineCommentWithQuotesData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id"
        -- This is a single-line comment with 'quotes' and "more quotes"
        FROM "dbo"."Products"
        """
    ])];

    public static TheoryData<SqlTestCase> StringLiteralWithCommentTokensData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id"
        FROM "dbo"."Products"
        WHERE "dbo"."Products"."PROD_NAME" = 'Item /* Note */ -- 1'
        """
    ])];

    public static TheoryData<SqlTestCase> KeywordsInLiteralData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id"
        FROM "dbo"."Products"
        WHERE "dbo"."Products"."PROD_NAME" = 'INSERT VALUES RETURNING FOR UPDATE'
        """
    ])];

    public static TheoryData<SqlTestCase> KeywordsInCommentData => [new SqlTestCase([
        """
        SELECT "dbo"."Products"."Id"
        FROM "dbo"."Products"
        /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
        """
    ])];
}