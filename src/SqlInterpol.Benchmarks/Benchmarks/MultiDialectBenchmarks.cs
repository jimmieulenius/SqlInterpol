using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
public class MultiDialectBenchmarks
{
    [Benchmark]
    public string SqlServer()
    {
        var db = SqlBuilder.SqlServer();
        BuildQuery(db);
        return db.Build().Sql;
    }

    [Benchmark(Baseline = true)]
    public string PostgreSql()
    {
        var db = SqlBuilder.PostgreSql();
        BuildQuery(db);
        return db.Build().Sql;
    }

    [Benchmark]
    public string MySql()
    {
        var db = SqlBuilder.MySql();
        BuildQuery(db);
        return db.Build().Sql;
    }

    [Benchmark]
    public string SqLite()
    {
        var db = SqlBuilder.SqLite();
        BuildQuery(db);
        return db.Build().Sql;
    }

    [Benchmark]
    public string Oracle()
    {
        var db = SqlBuilder.Oracle();
        BuildQuery(db);
        return db.Build().Sql;
    }

    private static void BuildQuery(SqlBuilder db)
    {
        int categoryId = 3;
        decimal minPrice = 9.99m;

        db.Entity<Product>(out var p);
        db.Entity<OrderLine>(out var ol);

        // The lexer dynamically detects 'AS p' and 'AS ol' now natively!
        db.Append($"""
            SELECT {p.Id}, {p.Name}, SUM({ol.Price}) AS total
            FROM {p} AS p
            JOIN {ol} AS ol ON {p.Id} = {ol.ProductId}
            WHERE {p.CategoryId} = {categoryId}
              AND {p.Price} >= {minPrice}
            GROUP BY {p.Id}, {p.Name}
            """);
    }
}