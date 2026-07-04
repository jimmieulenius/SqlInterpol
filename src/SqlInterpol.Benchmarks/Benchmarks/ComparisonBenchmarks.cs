using BenchmarkDotNet.Attributes;
using DapperSB = Dapper.SqlBuilder;
using SqlInterpol.Benchmarks.Models;
using SqlKata;
using SqlKata.Compilers;
using System.Text;

namespace SqlInterpol.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
public class ComparisonBenchmarks
{
    private readonly int _categoryId = 3;
    private readonly decimal _minPrice = 9.99m;
    private readonly bool _isActive = true;
    private readonly int _customerId = 42;
    private readonly int[] _filterIds = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    // Static Pre-Compiled Templates (New API)
    private static readonly ISqlTemplate _filteredSelectTemplate;
    private static readonly ISqlTemplate _aliasedJoinTemplate;
    private static readonly PostgresCompiler _postgresCompiler = new PostgresCompiler();

    static ComparisonBenchmarks()
    {
        var db = SqlBuilder.PostgreSql();
        
        db.Entity<Product>(out var p);
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
        db.Template(out _aliasedJoinTemplate, $$"""
            SELECT {{o.Id}}, {{o.CustomerId}}, {{ol.Price}}, {{ol.Quantity}} 
            FROM {{o}} AS o 
            JOIN {{ol}} AS ol ON {{o.Id}} = {{ol.OrderId}} 
            WHERE {{o.CustomerId}} = {{Sql.Arg("CustomerId")}}
            """);
    }

    [Benchmark(Baseline = true)]
    public string RawString() =>
        "SELECT \"Id\", \"PROD_NAME\", \"Price\" FROM \"dbo\".\"Products\" WHERE \"CategoryId\" = @p0 AND \"Price\" >= @p1 AND \"IsActive\" = @p2";

    [Benchmark]
    public string DapperSqlBuilder()
    {
        var builder = new DapperSB();
        var template = builder.AddTemplate("SELECT Id, PROD_NAME, Price FROM Products /**where**/");
        builder.Where("CategoryId = @categoryId", new { categoryId = _categoryId });
        builder.Where("Price >= @minPrice", new { minPrice = _minPrice });
        builder.Where("IsActive = @isActive", new { isActive = _isActive });
        return template.RawSql;
    }

    [Benchmark]
    public string SqlKata_PostgreSql()
    {
        var query = new Query("dbo.Products")
            .Select("Id", "PROD_NAME", "Price")
            .Where("CategoryId", _categoryId)
            .Where("Price", ">=", _minPrice)
            .Where("IsActive", _isActive);

        return _postgresCompiler.Compile(query).Sql;
    }

    [Benchmark]
    public string SqlInterpol_PostgreSql()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        db.Append($"SELECT {p.Id}, {p.Name}, {p.Price} FROM {p} WHERE {p.CategoryId} = {_categoryId} AND {p.Price} >= {_minPrice} AND {p.IsActive} = {_isActive}");
        return db.Build().Sql;
    }

    [Benchmark]
    public string SqlInterpolTemplate_PostgreSql()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_filteredSelectTemplate, new { CategoryId = _categoryId, MinPrice = _minPrice, IsActive = _isActive });
        return db.Build().Sql;
    }

    [Benchmark]
    public string RawString_InClause()
    {
        var sb = new StringBuilder("SELECT \"Id\", \"PROD_NAME\" FROM \"dbo\".\"Products\" WHERE \"CategoryId\" = @p0 AND \"Id\" IN (");
        for (int i = 0; i < _filterIds.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append($"@p{i + 1}");
        }
        sb.Append(')');
        return sb.ToString();
    }

    [Benchmark]
    public string SqlKata_InClause()
    {
        var query = new Query("dbo.Products")
            .Select("Id", "PROD_NAME")
            .Where("CategoryId", _categoryId)
            .WhereIn("Id", _filterIds);

        return _postgresCompiler.Compile(query).Sql;
    }

    [Benchmark]
    public string SqlInterpol_InClause()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        db.Append($"SELECT {p.Id}, {p.Name} FROM {p} WHERE {p.CategoryId} = {_categoryId} AND {p.Id} IN ({_filterIds})");
        return db.Build().Sql;
    }

    [Benchmark]
    public string RawString_AliasedJoin() =>
        "SELECT o.\"Id\", o.\"CustomerId\", ol.\"Price\", ol.\"Quantity\" FROM \"dbo\".\"Orders\" AS o JOIN \"dbo\".\"OrderLines\" AS ol ON o.\"Id\" = ol.\"OrderId\" WHERE o.\"CustomerId\" = @p0";

    [Benchmark]
    public string SqlKata_AliasedJoin()
    {
        var query = new Query("dbo.Orders AS o")
            .Select("o.Id", "o.CustomerId", "ol.Price", "ol.Quantity")
            .Join("dbo.OrderLines AS ol", "o.Id", "ol.OrderId")
            .Where("o.CustomerId", _customerId);

        return _postgresCompiler.Compile(query).Sql;
    }

    [Benchmark]
    public string SqlInterpol_AliasedJoin()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Order>(out var o);
        db.Entity<OrderLine>(out var ol);
        db.Append($"SELECT {o.Id}, {o.CustomerId}, {ol.Price}, {ol.Quantity} FROM {o} AS o JOIN {ol} AS ol ON {o.Id} = {ol.OrderId} WHERE {o.CustomerId} = {_customerId}");
        return db.Build().Sql;
    }

    [Benchmark]
    public string SqlInterpolTemplate_AliasedJoin()
    {
        var db = SqlBuilder.PostgreSql();
        db.Append(_aliasedJoinTemplate, new { CustomerId = _customerId });
        return db.Build().Sql;
    }
}