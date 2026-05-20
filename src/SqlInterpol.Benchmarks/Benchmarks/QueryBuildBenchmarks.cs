using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

/// <summary>
/// Measures query building cost across varying query complexity levels.
/// All benchmarks use PostgreSQL to isolate the building/rendering pipeline.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
public class QueryBuildBenchmarks
{
    private readonly int _categoryId = 3;
    private readonly int _customerId = 42;
    private readonly decimal _minPrice = 9.99m;

    /// <summary>Single-entity SELECT with one parameter.</summary>
    [Benchmark(Baseline = true)]
    public string SimpleSelect()
    {
        var db = SqlBuilder.PostgreSql();
        var p = db.AddEntity<Product>();

        db.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]}, {p[x => x.Price]} FROM {p} WHERE {p[x => x.CategoryId]} = {_categoryId}");

        return db.Build().Sql;
    }

    /// <summary>Single-entity SELECT with multiple WHERE conditions and parameters.</summary>
    [Benchmark]
    public string FilteredSelect()
    {
        var db = SqlBuilder.PostgreSql();
        var p = db.AddEntity<Product>();

        db.Append($"""
            SELECT {p[x => x.Id]}, {p[x => x.Name]}, {p[x => x.Price]}
            FROM {p}
            WHERE {p[x => x.CategoryId]} = {_categoryId}
              AND {p[x => x.Price]} >= {_minPrice}
              AND {p[x => x.IsActive]} = {true}
            """);

        return db.Build().Sql;
    }

    /// <summary>Two-entity JOIN with aliasing and multiple parameters.</summary>
    [Benchmark]
    public string JoinQuery()
    {
        var db = SqlBuilder.PostgreSql();
        var o = db.AddEntity<Order>(alias: "o");
        var ol = db.AddEntity<OrderLine>(alias: "ol");

        db.Append($"""
            SELECT {o[x => x.Id]}, {o[x => x.Total]}, {ol[x => x.Price]}, {ol[x => x.Quantity]}
            FROM {o}
            JOIN {ol} ON {o[x => x.Id]} = {ol[x => x.OrderId]}
            WHERE {o[x => x.CustomerId]} = {_customerId}
            """);

        return db.Build().Sql;
    }

    /// <summary>Three-entity JOIN with subquery-style aggregation and paging.</summary>
    [Benchmark]
    public string ComplexJoinWithPaging()
    {
        var db = SqlBuilder.PostgreSql();
        var o = db.AddEntity<Order>(alias: "o");
        var ol = db.AddEntity<OrderLine>(alias: "ol");
        var p = db.AddEntity<Product>(alias: "p");

        int page = 2;
        int pageSize = 20;
        int offset = (page - 1) * pageSize;

        db.Append($"""
            SELECT {o[x => x.Id]}, {o[x => x.Total]}, {p[x => x.Name]}, SUM({ol[x => x.Price]}) AS line_total
            FROM {o}
            JOIN {ol} ON {o[x => x.Id]} = {ol[x => x.OrderId]}
            JOIN {p} ON {ol[x => x.ProductId]} = {p[x => x.Id]}
            WHERE {o[x => x.CustomerId]} = {_customerId}
              AND {p[x => x.IsActive]} = {true}
            GROUP BY {o[x => x.Id]}, {o[x => x.Total]}, {p[x => x.Name]}
            ORDER BY {o[x => x.Total]} DESC
            LIMIT {pageSize} OFFSET {offset}
            """);

        return db.Build().Sql;
    }
}
