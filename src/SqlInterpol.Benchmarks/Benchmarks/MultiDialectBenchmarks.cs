using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

/// <summary>
/// Measures the rendering cost across all five supported dialects for the same logical query.
/// Highlights per-dialect overhead differences (identifier quoting, parameter styles, etc.).
/// </summary>
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

        var p = db.AddEntity<Product>(alias: "p");
        var ol = db.AddEntity<OrderLine>(alias: "ol");

        db.Append($"""
            SELECT {p[x => x.Id]}, {p[x => x.Name]}, SUM({ol[x => x.Price]}) AS total
            FROM {p}
            JOIN {ol} ON {p[x => x.Id]} = {ol[x => x.ProductId]}
            WHERE {p[x => x.CategoryId]} = {categoryId}
              AND {p[x => x.Price]} >= {minPrice}
            GROUP BY {p[x => x.Id]}, {p[x => x.Name]}
            """);
    }
}
