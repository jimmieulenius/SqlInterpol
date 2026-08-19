using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IFormattingTestSuite))]
public abstract partial class FormattingTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IFormattingTestSuite.Select_WithNewLinesData))]
    public void Select_WithNewLines(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($"""
                SELECT 
                    {p.Id}, 
                    {p.Name}
                FROM 
                    {p}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.Select_WithTabsData))]
    public void Select_WithTabs(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($"""
                SELECT  {p.Id},  {p.Name}
                FROM  {p}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.Select_WithExtraSpacesData))]
    public void Select_WithExtraSpaces(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            // Testing right-aligned keywords (common in some style guides)
            return db.Append($"""
                SELECT {p.Id}
                  FROM {p}
                 WHERE {p.Id} = 1
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.Select_WithMixedWhitespaceData))]
    public void Select_WithMixedWhitespace(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            // Testing a mix of leading/trailing blank lines and indentation
            return db.Append($"""

                    SELECT {p.Id}
                    FROM {p}

                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.Select_WithCommentsData))]
    public void Select_WithComments(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($"""
                SELECT {p.Id} -- This is the primary key
                FROM {p} /* This is the table */
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.InsertVerticalLayoutData))]
    public void Insert_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;

        // Act
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);
            var dto = new { Status = "New", Total = 10m };
            return db.Append($"""
                INSERT INTO {o}
                VALUES {dto}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.UpdateVerticalLayoutData))]
    public void Update_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;

        // Act
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);
            var dto = new { Status = "Processing", Total = 50.00m };
            // Note: No trailing space after SET because the vertical collection prepends its own newline!
            return db.Append($"""
                UPDATE {o}
                SET {dto}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.BulkInsertVerticalLayoutData))]
    public void BulkInsert_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            var products = new[]
            {
                new { Name = "Prod1", CategoryId = 1, Price = 10m },
                new { Name = "Prod2", CategoryId = 2, Price = 20m }
            };
            return db.Append($"""
                INSERT INTO {p}
                VALUES
                {products}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.WhereInVerticalLayoutData))]
    public void WhereIn_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;

        // Act
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);
            var ids = new[] { 1, 2, 3 };
            return db.Append($"""
                SELECT *
                FROM {o}
                WHERE {o.Id} IN (
                    {ids}
                )
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFormattingTestSuite.OrderByEnumerableVerticalLayoutData))]
    public void OrderBy_EnumerableCombiner_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;

        // Act
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);
            IEnumerable<ISqlOrderFragment> sorts = 
            [
                o.OrderBy("Total"),
                o.OrderBy(x => x.Id, SqlOrderDirection.Desc)
            ];

#pragma warning disable SQLIG10 // IEnumerable<ISqlOrderFragment> falls back to JIT evaluation
            return db.Append($"""
                SELECT *
                FROM {o}
                ORDER BY {sorts}
                """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    [SqlTest(nameof(IFormattingTestSuite.SelectEntityExpansionVerticalLayoutData))]
    public void Select_EntityExpansion_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;

        // Act
        testCase.Act(() =>
        {
            db.Entity<ProductWithIgnoreModel>(out var p);
            return db.Append($"""
                SELECT {p}
                FROM {p} AS p1
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}