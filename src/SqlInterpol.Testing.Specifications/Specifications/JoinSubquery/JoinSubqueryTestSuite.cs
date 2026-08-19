using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IJoinSubqueryTestSuite))]
public abstract partial class JoinSubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IJoinSubqueryTestSuite.JoinSubqueryData))]
    public void Join_Subquery_WithTypeSafeProjection(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<CategoryStats>(out var stats)
              .Entity<Product>(out var p, "p");

#pragma warning disable SQLIG10 // Injecting dynamic query fragments into JOIN clauses forces JIT fallback
            db.Query(stats, out var statsQuery, () => db.Append($$"""
                SELECT 
                    {{p.CategoryId}} AS {{stats.CategoryId}},
                    SUM({{p.Price}}) AS {{stats.TotalPrice}}
                FROM {{p}}
                GROUP BY {{p.CategoryId}}
                """));

            db.Entity<Category>(out var c, "c");

            return db.Append($$"""
                SELECT 
                    {{c.Name}}, 
                    {{stats.TotalPrice}}
                FROM {{c}}
                LEFT JOIN
                (
                    {{statsQuery}}
                ) AS stats
                    ON {{stats.CategoryId}} = {{c.Id}}
                """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
        // Omitted db.AssertAotIntercepted() because this complex subquery injection executes via JIT
    }
}