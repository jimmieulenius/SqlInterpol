using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;
using SqlInterpol.Parsing;

namespace SqlInterpol.Benchmarks;

// The AOT Analyzer explicitly looks for "Append" or "AppendLine".
// By renaming the call to "AppendJit", the analyzer skips interception.
public static class JitExtensions
{
    public static SqlBuilder AppendJit(
        this SqlBuilder builder, 
        [InterpolatedStringHandlerArgument("builder")] ref SqlQueryInterpolatedStringHandler handler)
    {
        // Because 'handler' is a variable reference here (not an inline string literal),
        // the AOT generator safely emits its fallback: builder.Append(ref handler);
        return builder.Append(ref handler);
    }
}

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

    // --- SIMPLE SELECT ---

    [Benchmark(Baseline = true)]
    public string SimpleSelect_JIT()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        db.AppendJit($"SELECT {p.Id}, {p.Name}, {p.Price} FROM {p} WHERE {p.CategoryId} = {_categoryId}");
        return db.Build().Sql;
    }

    [Benchmark]
    public string SimpleSelect_AOT()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        // Intercepted natively by the AOT compiler
        db.Append($"SELECT {p.Id}, {p.Name}, {p.Price} FROM {p} WHERE {p.CategoryId} = {_categoryId}");
        return db.Build().Sql;
    }

    [Benchmark]
    public string SimpleSelect_Template()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_simpleSelectTemplate, new { CategoryId = _categoryId });
        return db.Build().Sql;
    }

    // --- FILTERED SELECT ---

    [Benchmark]
    public string FilteredSelect_JIT()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        db.AppendJit($"""
            SELECT {p.Id}, {p.Name}, {p.Price}
            FROM {p}
            WHERE {p.CategoryId} = {_categoryId}
              AND {p.Price} >= {_minPrice}
              AND {p.IsActive} = {true}
            """);
        return db.Build().Sql;
    }

    [Benchmark]
    public string FilteredSelect_AOT()
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
    public string FilteredSelect_Template()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_filteredSelectTemplate, new { CategoryId = _categoryId, MinPrice = _minPrice, IsActive = true });
        return db.Build().Sql;
    }

    // --- JOIN QUERY ---

    [Benchmark]
    public string JoinQuery_JIT()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Order>(out var o);
        db.Entity<OrderLine>(out var ol);
        db.AppendJit($"""
            SELECT {o.Id}, {o.Total}, {ol.Price}, {ol.Quantity}
            FROM {o} AS o
            JOIN {ol} AS ol ON {o.Id} = {ol.OrderId}
            WHERE {o.CustomerId} = {_customerId}
            """);
        return db.Build().Sql;
    }

    [Benchmark]
    public string JoinQuery_AOT()
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
    public string JoinQuery_Template()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_joinTemplate, new { CustomerId = _customerId });
        return db.Build().Sql;
    }
}