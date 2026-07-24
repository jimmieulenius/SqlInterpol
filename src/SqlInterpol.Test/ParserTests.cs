using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
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
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}
            FROM {{p}}
            WHERE {{p.Name}} = 'O''Connor' AND {{p.CategoryId}} = 1
            """)
            .Build()
        );

        // Assert - testCase.Assert() automatically validates SQL and 0 parameters
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(ParameterizedQuotesData))]
    public void StringHandling_ParameterizedQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var searchName = "O'Connor"; // A malicious or complex string

        // Act - Testing that variables are parameterized, naturally escaping the risk of injection
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}
            FROM {{p}}
            WHERE {{p.Name}} = {{searchName}}
            """)
            .Build()
        );

        // Assert - testCase.Assert() automatically validates SQL and the expected parameter value
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(MultiLineCommentWithQuotesData))]
    public void StringHandling_MultiLineCommentWithQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing that quotes inside a multi-line comment do not start a string state
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}
            /* This is a multi-line comment.
               It has 'single quotes' and "double quotes".
               The parser should completely ignore them.
            */
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(SingleLineCommentWithQuotesData))]
    public void StringHandling_SingleLineCommentWithQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing that quotes inside a single-line comment do not start a string state
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}
            -- This is a single-line comment with 'quotes' and "more quotes"
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(StringLiteralWithCommentTokensData))]
    public void StringHandling_StringLiteralWithCommentTokens(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing that comment tokens inside a string literal do not start a comment state
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}
            FROM {{p}}
            WHERE {{p.Name}} = 'Item /* Note */ -- 1'
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(KeywordsInLiteralData))]
    public void StringHandling_KeywordsInsideLiteral_AreIgnored(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Proof that AST keyword interceptors (like FOR UPDATE or RETURNING) ignore strings
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}
            FROM {{p}}
            WHERE {{p.Name}} = 'INSERT VALUES RETURNING FOR UPDATE'
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(KeywordsInCommentData))]
    public void StringHandling_KeywordsInsideComment_AreIgnored(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Proof that AST keyword interceptors ignore comments
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}
            FROM {{p}}
            /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    // 1. Standard Replacements & Case Insensitivity
    [InlineData("SELECT 1 EXCEPT SELECT 2", "SELECT 1 MINUS SELECT 2")]
    [InlineData("SELECT 1 except SELECT 2", "SELECT 1 MINUS SELECT 2")]
    [InlineData("EXCEPT EXCEPT", "MINUS MINUS")]
    [InlineData("EXCEPT\nEXCEPT", "MINUS\nMINUS")]
    
    // 2. Word Boundaries (Must not replace substrings)
    [InlineData("SELECT UNEXCEPTED", "SELECT UNEXCEPTED")]
    [InlineData("SELECT EXCEPTIONAL", "SELECT EXCEPTIONAL")]
    [InlineData("SELECT MY_EXCEPT_COLUMN", "SELECT MY_EXCEPT_COLUMN")]
    
    // 3. String Escapes (Must ignore keywords inside quotes)
    [InlineData("SELECT 'Everyone EXCEPT you'", "SELECT 'Everyone EXCEPT you'")]
    [InlineData("SELECT 'It''s EXCEPT here'", "SELECT 'It''s EXCEPT here'")]
    [InlineData("EXCEPT 'EXCEPT' EXCEPT", "MINUS 'EXCEPT' MINUS")]
    
    // 4. Line Comments (Must ignore keywords after --)
    [InlineData("SELECT 1 -- EXCEPT this", "SELECT 1 -- EXCEPT this")]
    [InlineData("SELECT 1 -- EXCEPT\n EXCEPT", "SELECT 1 -- EXCEPT\n MINUS")]
    
    // 5. Block Comments (Must ignore keywords inside /* */)
    [InlineData("SELECT 1 /* EXCEPT */", "SELECT 1 /* EXCEPT */")]
    [InlineData("EXCEPT /* EXCEPT */ EXCEPT", "MINUS /* EXCEPT */ MINUS")]
    
    // 6. The Ultimate Mixed Query
    [InlineData(
        "EXCEPT 'EXCEPT' /* EXCEPT */ -- EXCEPT\n EXCEPT", 
        "MINUS 'EXCEPT' /* EXCEPT */ -- EXCEPT\n MINUS")]
    public void ReplaceKeyword_SafelyReplacesTarget(string input, string expected)
    {
        // Act
        var result = SqlSegmentPreprocessor.SafeReplaceKeyword(input, "EXCEPT", "MINUS");

        // Assert
        Assert.Equal(expected, result);
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'O''Connor' AND "dbo"."Products"."CategoryId" = 1
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

    public static TheoryData<SqlTestCase> ParameterizedQuotesData
    {
        get
        {
            object?[] expectedParams = ["O'Connor"];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT <<dbo>>.<<Products>>.<<Id>>
                        FROM <<dbo>>.<<Products>>
                        WHERE <<dbo>>.<<Products>>.<<PROD_NAME>> = !!100
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."PROD_NAME" = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT `dbo`.`Products`.`Id`
                        FROM `dbo`.`Products`
                        WHERE `dbo`.`Products`.`PROD_NAME` = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."PROD_NAME" = :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."PROD_NAME" = $1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."PROD_NAME" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id]
                        FROM [dbo].[Products]
                        WHERE [dbo].[Products].[PROD_NAME] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

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
            SqlDialectKind.Firebird,
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id"
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM "dbo"."Products"
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'Item /* Note */ -- 1'
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."PROD_NAME" = 'INSERT VALUES RETURNING FOR UPDATE'
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"
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