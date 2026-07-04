using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
public class QueryBuildBenchmarks
{
    private readonly int _categoryId = 3;
    private readonly int _customerId = 42;
    private readonly decimal _minPrice = 9.99m;

    private static ISqlTemplate _simpleSelectTemplate;
    private static ISqlTemplate _filteredSelectTemplate;
    private static ISqlTemplate _joinTemplate;

    [GlobalSetup]
    public void Setup()
    {
        var db = SqlBuilder.PostgreSql();
        
        db.Entity<Product>(out var p);
        db.Template(out _simpleSelectTemplate, $$"""
            SELECT {{p.Id}}, {{p.Name}}, {{p.Price}} 
            FROM {{p}} 
            WHERE {{p.CategoryId}} = {{Sql.Arg("CategoryId")}}
            """);

        db.Clear();
        
        db.Entity<Product>(out p);
        db.Template(out _filteredSelectTemplate, $$"""
            SELECT {{p.Id}}, {{p.Name}}, {{p.Price}}
            FROM {{p}}
            WHERE {{p.CategoryId}} = {{Sql.Arg("CategoryId")}}
              AND {{p.Price}} >= {{Sql.Arg("MinPrice")}}
              AND {{p.IsActive}} = {{Sql.Arg("IsActive")}}
            """);

        db.Clear();
        
        db.Entity<Order>(out var o);
        db.Entity<OrderLine>(out var ol);
        db.Template(out _joinTemplate, $$"""
            SELECT {{o.Id}}, {{o.Total}}, {{ol.Price}}, {{ol.Quantity}}
            FROM {{o}} AS o
            JOIN {{ol}} AS ol ON {{o.Id}} = {{ol.OrderId}}
            WHERE {{o.CustomerId}} = {{Sql.Arg("CustomerId")}}
            """);
    }

    [Benchmark(Baseline = true)]
    public string SimpleSelect()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        db.Append($"SELECT {p.Id}, {p.Name}, {p.Price} FROM {p} WHERE {p.CategoryId} = {_categoryId}");
        return db.Build().Sql;
    }

    [Benchmark]
    public string Template_SimpleSelect()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_simpleSelectTemplate, new { CategoryId = _categoryId });
        return db.Build().Sql;
    }

    [Benchmark]
    public string FilteredSelect()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        db.Append($"""
            SELECT {p.Id}, {p.Name}, {p.Price}
            FROM {p}
            WHERE {p.CategoryId} = {_categoryId}
              AND {p.Price} >= {_minPrice}
              AND {p.IsActive} = {true}
            """);
        return db.Build().Sql;
    }

    [Benchmark]
    public string Template_FilteredSelect()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_filteredSelectTemplate, new { CategoryId = _categoryId, MinPrice = _minPrice, IsActive = true });
        return db.Build().Sql;
    }

    [Benchmark]
    public string JoinQuery()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Order>(out var o);
        db.Entity<OrderLine>(out var ol);

        db.Append($"""
            SELECT {o.Id}, {o.Total}, {ol.Price}, {ol.Quantity}
            FROM {o} AS o
            JOIN {ol} AS ol ON {o.Id} = {ol.OrderId}
            WHERE {o.CustomerId} = {_customerId}
            """);
        return db.Build().Sql;
    }

    [Benchmark]
    public string Template_JoinQuery()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_joinTemplate, new { CustomerId = _customerId });
        return db.Build().Sql;
    }

    [Benchmark]
    public string ComplexJoinWithPaging()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Order>(out var o);
        db.Entity<OrderLine>(out var ol);
        db.Entity<Product>(out var p);

        int page = 2;
        int pageSize = 20;
        int offset = (page - 1) * pageSize;

        db.Append($"""
            SELECT {o.Id}, {o.Total}, {p.Name}, SUM({ol.Price}) AS line_total
            FROM {o} AS o
            JOIN {ol} AS ol ON {o.Id} = {ol.OrderId}
            JOIN {p} AS p ON {ol.ProductId} = {p.Id}
            WHERE {o.CustomerId} = {_customerId}
              AND {p.IsActive} = {true}
            GROUP BY {o.Id}, {o.Total}, {p.Name}
            ORDER BY {o.Total} DESC
            LIMIT {pageSize} OFFSET {offset}
            """);
        return db.Build().Sql;
    }
}