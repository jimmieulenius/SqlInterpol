using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IFromSubqueryTestSuite))]
public abstract partial class FromSubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IFromSubqueryTestSuite.From_SubqueryData))]
    public void From_Subquery(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<CategoryStats>(out var stats)
              .Entity<Product>(out var p, "p");

            db.Query(
                stats,
                () => db.Append($"""
                    SELECT
                        {p.CategoryId} AS {stats.CategoryId:alias},
                        SUM({p.Price}) AS {stats.TotalPrice:alias}
                    FROM {p}
                    GROUP BY {p.CategoryId}
                    """));

            db.Entity<Category>(out var c, "c");
            
            return db.Append($"""
                SELECT
                    {c.Name},
                    {stats.TotalPrice}
                FROM
                (
                    {stats}
                ) AS stats
                JOIN {c} ON {stats.CategoryId} = {c.Id}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromSubqueryTestSuite.From_SubqueryData))]
    public void From_Subquery_AutoAliasing(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.EntityAutoAliasing = true;

        // Act
        testCase.Act(() =>
        {
            db.Entity<CategoryStats>(out var stats)
              .Entity<Product>(out var p);

            db.Query(
                stats,
                () => db.Append($"""
                    SELECT
                        {p.CategoryId} AS {stats.CategoryId:alias},
                        SUM({p.Price}) AS {stats.TotalPrice:alias}
                    FROM {p}
                    GROUP BY {p.CategoryId}
                """));

            db.Entity<Category>(out var c);
            
            return db.Append($"""
                SELECT
                    {c.Name},
                    {stats.TotalPrice}
                FROM
                {stats}
                JOIN {c:decl} ON {stats.CategoryId} = {c.Id}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}