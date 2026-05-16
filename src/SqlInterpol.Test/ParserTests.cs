using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class ParserTests
{
    [Theory]
    [MemberData(nameof(EscapedQuotesInLiteralData))]
    public void StringHandling_EscapedQuotesInSqlLiteral(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing the parser's ability to read a raw SQL string containing '' 
        // without prematurely ending the string token.
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
            FROM {{p}}
            WHERE {{p[x => x.Name]}} = 'O''Connor' AND {{p[x => x.CategoryId]}} = 1
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
        
        // Assert Parameters - There should be 0 parameters since everything was raw text
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [MemberData(nameof(ParameterizedQuotesData))]
    public void StringHandling_ParameterizedQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var searchName = "O'Connor"; // A malicious or complex string

        // Act - Testing that variables are parameterized, naturally escaping the risk of injection
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
            FROM {{p}}
            WHERE {{p[x => x.Name]}} = {{searchName}}
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);

        // Assert Parameters
        Assert.Single(result.Parameters);
        Assert.Equal("O'Connor", result.Parameters.ElementAt(0).Value);
    }

    [Theory]
    [MemberData(nameof(MultiLineCommentWithQuotesData))]
    public void StringHandling_MultiLineCommentWithQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing that quotes inside a multi-line comment do not start a string state
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
            /* This is a multi-line comment.
               It has 'single quotes' and "double quotes".
               The parser should completely ignore them.
            */
            FROM {{p}}
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [MemberData(nameof(SingleLineCommentWithQuotesData))]
    public void StringHandling_SingleLineCommentWithQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing that quotes inside a single-line comment do not start a string state
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
            -- This is a single-line comment with 'quotes' and "more quotes"
            FROM {{p}}
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [MemberData(nameof(StringLiteralWithCommentTokensData))]
    public void StringHandling_StringLiteralWithCommentTokens(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing that comment tokens inside a string literal do not start a comment state
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
            FROM {{p}}
            WHERE {{p[x => x.Name]}} = 'Item /* Note */ -- 1'
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [MemberData(nameof(KeywordsInLiteralData))]
    public void StringHandling_KeywordsInsideLiteral_AreIgnored(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Proof that AST keyword interceptors (like FOR UPDATE or RETURNING) ignore strings
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
            FROM {{p}}
            WHERE {{p[x => x.Name]}} = 'INSERT VALUES RETURNING FOR UPDATE'
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(KeywordsInCommentData))]
    public void StringHandling_KeywordsInsideComment_AreIgnored(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Proof that AST keyword interceptors ignore comments
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
            FROM {{p}}
            /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> EscapedQuotesInLiteralData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<PROD_NAME>> = 'O''Connor' AND <<dbo>>.<<Products>>.<<CategoryId>> = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`PROD_NAME` = 'O''Connor' AND `dbo`.`Products`.`CategoryId` = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'O''Connor' AND "dbo"."Products"."CategoryId" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'O''Connor' AND "dbo"."Products"."CategoryId" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'O''Connor' AND "dbo"."Products"."CategoryId" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[PROD_NAME] = 'O''Connor' AND [dbo].[Products].[CategoryId] = 1
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> ParameterizedQuotesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<PROD_NAME>> = !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`PROD_NAME` = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = ?0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[PROD_NAME] = @p0
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> MultiLineCommentWithQuotesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                /* This is a multi-line comment.
                   It has 'single quotes' and "double quotes".
                   The parser should completely ignore them.
                */
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                /* This is a multi-line comment.
                   It has 'single quotes' and "double quotes".
                   The parser should completely ignore them.
                */
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                /* This is a multi-line comment.
                   It has 'single quotes' and "double quotes".
                   The parser should completely ignore them.
                */
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                /* This is a multi-line comment.
                   It has 'single quotes' and "double quotes".
                   The parser should completely ignore them.
                */
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                /* This is a multi-line comment.
                   It has 'single quotes' and "double quotes".
                   The parser should completely ignore them.
                */
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                /* This is a multi-line comment.
                   It has 'single quotes' and "double quotes".
                   The parser should completely ignore them.
                */
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SingleLineCommentWithQuotesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> StringLiteralWithCommentTokensData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<PROD_NAME>> = 'Item /* Note */ -- 1'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`PROD_NAME` = 'Item /* Note */ -- 1'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'Item /* Note */ -- 1'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'Item /* Note */ -- 1'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'Item /* Note */ -- 1'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[PROD_NAME] = 'Item /* Note */ -- 1'
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> KeywordsInLiteralData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<PROD_NAME>> = 'INSERT VALUES RETURNING FOR UPDATE'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`PROD_NAME` = 'INSERT VALUES RETURNING FOR UPDATE'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'INSERT VALUES RETURNING FOR UPDATE'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'INSERT VALUES RETURNING FOR UPDATE'
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'INSERT VALUES RETURNING FOR UPDATE'
                """
            ]
        ),
        // Notice: SQL Server does NOT rewrite the 'FOR UPDATE' here! It stays exact.
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[PROD_NAME] = 'INSERT VALUES RETURNING FOR UPDATE'
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> KeywordsInCommentData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                FROM `dbo`.`Products`
                /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                FROM [dbo].[Products]
                /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
                """
            ]
        )
    ];
}