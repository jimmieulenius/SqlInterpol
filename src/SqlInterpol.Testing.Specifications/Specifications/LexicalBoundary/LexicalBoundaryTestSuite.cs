using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ILexicalBoundaryTestSuite))]
public abstract partial class LexicalBoundaryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ILexicalBoundaryTestSuite.EscapedQuotesInLiteralData))]
    public void StringHandling_EscapedQuotesInSqlLiteral(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}
                FROM {{p}}
                WHERE {{p.Name}} = 'O''Connor' AND {{p.CategoryId}} = 1
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ILexicalBoundaryTestSuite.ParameterizedQuotesData))]
    public void StringHandling_ParameterizedQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var searchName = "O'Connor";

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}
                FROM {{p}}
                WHERE {{p.Name}} = {{searchName}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ILexicalBoundaryTestSuite.MultiLineCommentWithQuotesData))]
    public void StringHandling_MultiLineCommentWithQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}
                /* This is a multi-line comment.
                   It has 'single quotes' and "double quotes".
                   The parser should completely ignore them.
                */
                FROM {{p}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ILexicalBoundaryTestSuite.SingleLineCommentWithQuotesData))]
    public void StringHandling_SingleLineCommentWithQuotes(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}
                -- This is a single-line comment with 'quotes' and "more quotes"
                FROM {{p}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ILexicalBoundaryTestSuite.StringLiteralWithCommentTokensData))]
    public void StringHandling_StringLiteralWithCommentTokens(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}
                FROM {{p}}
                WHERE {{p.Name}} = 'Item /* Note */ -- 1'
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ILexicalBoundaryTestSuite.KeywordsInLiteralData))]
    public void StringHandling_KeywordsInsideLiteral_AreIgnored(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}
                FROM {{p}}
                WHERE {{p.Name}} = 'INSERT VALUES RETURNING FOR UPDATE'
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ILexicalBoundaryTestSuite.KeywordsInCommentData))]
    public void StringHandling_KeywordsInsideComment_AreIgnored(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}
                FROM {{p}}
                /* We don't want to INSERT VALUES RETURNING FOR UPDATE here */
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}