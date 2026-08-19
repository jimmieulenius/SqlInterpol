using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ICteTestSuite))]
public abstract partial class CteTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder();

    protected const int Threshold = 100;

    [SqlTest(nameof(ICteTestSuite.Select_WithCteData))]
    public void Select_WithCte(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            #pragma warning disable SQLIG10
            db.Entity<Product>(out var p)
              .Query(p, out var innerQuery, () => db.Append($$"""
                  SELECT {{p.CategoryId}}, SUM({{p.Price}}) AS TotalPrice
                  FROM {{p}} AS {{"p"}}
                  GROUP BY {{p.CategoryId}}
                  """));

            db.Entity<Category>(out var c);
            db.Entity<CategoryStats>(out var cs);

            return db.Append($$"""
                WITH {{cs}} AS (
                    {{innerQuery}}
                )
                SELECT {{c.Name}}, {{cs.TotalPrice}}
                FROM {{c}} AS {{"c"}}
                JOIN {{cs}} AS {{"cs"}}
                    ON {{c.Id}} = {{cs.CategoryId}}
                """).Build();
            #pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    [SqlTest(nameof(ICteTestSuite.Select_WithCteData))]
    public void Select_WithCte_AutoAliased(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act
        testCase.Act(() => 
        {
            #pragma warning disable SQLIG10
            db.Context.Options.EntityAutoAliasing = true;

            // Define the inner query for the CTE
            var innerQuery = db
                .Entity<Product>(out var p)
                .Query(p, () => db.Append($$"""
                    SELECT {{p.CategoryId}}, SUM({{p.Price}}) AS TotalPrice
                    FROM {{p}}
                    GROUP BY {{p.CategoryId}}
                    """));

            db.Entity<Category>(out var c);
            db.Entity<CategoryStats>(out var cs);

            // Auto-aliasing detects the FROM and JOIN locations and injects "AS c" and "AS cs" automatically!
            return db.Append($$"""
                WITH {{cs}} AS (
                    {{innerQuery}}
                )
                SELECT {{c.Name}}, {{cs.TotalPrice}}
                FROM {{c}}
                JOIN {{cs}}
                    ON {{c.Id}} = {{cs.CategoryId}}
                """).Build();
            #pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    [SqlTest(nameof(ICteTestSuite.Select_WithRecursiveCteData))]
    public void Select_WithRecursiveCte(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
            db.Append($"""
                WITH RECURSIVE Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ICteTestSuite.RawCteData))]
    public void Select_WithRawCte(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);

            return db.Append($$"""
                WITH ExpensiveProducts AS (
                    SELECT * FROM {{p}} WHERE {{p.Price}} > {{Threshold}}
                )
                SELECT * FROM ExpensiveProducts
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}