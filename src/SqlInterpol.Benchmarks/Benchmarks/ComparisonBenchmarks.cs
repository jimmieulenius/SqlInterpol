using BenchmarkDotNet.Attributes;
using DapperSB = Dapper.SqlBuilder;
using SqlInterpol.Benchmarks.Models;
using SqlKata;
using SqlKata.Compilers;
using System.Text;

namespace SqlInterpol.Benchmarks;

/// <summary>
/// Compares SqlInterpol against raw strings, Dapper.SqlBuilder, and SqlKata across three scenarios:
/// 1. Simple filtered SELECT (baseline)
/// 2. IN (...) clause with a variable-size collection (parameter expansion)
/// 3. Aliased multi-table JOIN (alias resolution)
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
public class ComparisonBenchmarks
{
    private readonly int _categoryId = 3;
    private readonly decimal _minPrice = 9.99m;
    private readonly bool _isActive = true;
    private readonly int _customerId = 42;
    private readonly int[] _filterIds = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    // Static Pre-Compiled Templates
    private static readonly SqlTemplate<Product> _filteredSelectTemplate = SqlTemplate.Create<Product>((db, p) =>
        db.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]}, {p[x => x.Price]} FROM {p} WHERE {p[x => x.CategoryId]} = {Sql.Arg("CategoryId")} AND {p[x => x.Price]} >= {Sql.Arg("MinPrice")} AND {p[x => x.IsActive]} = {Sql.Arg("IsActive")}"));

    private static readonly SqlTemplate<Order, OrderLine> _aliasedJoinTemplate = SqlTemplate.Create<Order, OrderLine>((db, o, ol) =>
        db.Append($"SELECT {o[x => x.Id]}, {o[x => x.CustomerId]}, {ol[x => x.Price]}, {ol[x => x.Quantity]} FROM {o} JOIN {ol} ON {o[x => x.Id]} = {ol[x => x.OrderId]} WHERE {o[x => x.CustomerId]} = {Sql.Arg("CustomerId")}"));

    // SqlKata Compiler (Instantiated once to match fairness with static caches)
    private static readonly PostgresCompiler _postgresCompiler = new PostgresCompiler();

    // -------------------------------------------------------------------------
    // Scenario 1: Simple filtered SELECT
    // -------------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    public string RawString() =>
        "SELECT \"Id\", \"PROD_NAME\", \"Price\" FROM \"dbo\".\"Products\" WHERE \"CategoryId\" = @p0 AND \"Price\" >= @p1 AND \"IsActive\" = @p2";

    [Benchmark]
    public string DapperSqlBuilder()
    {
        var builder = new DapperSB();
        var template = builder.AddTemplate(
            "SELECT Id, PROD_NAME, Price FROM Products /**where**/");
        builder.Where("CategoryId = @categoryId", new { categoryId = _categoryId });
        builder.Where("Price >= @minPrice", new { minPrice = _minPrice });
        builder.Where("IsActive = @isActive", new { isActive = _isActive });
        return template.RawSql;
    }

    /// <summary>
    /// SqlKata — Fluent API building an AST, then compiled by PostgresCompiler.
    /// Does not use reflection for column names (requires magic strings).
    /// </summary>
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
        var p = db.AddEntity<Product>();
        db.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]}, {p[x => x.Price]} FROM {p} WHERE {p[x => x.CategoryId]} = {_categoryId} AND {p[x => x.Price]} >= {_minPrice} AND {p[x => x.IsActive]} = {_isActive}");
        return db.Build().Sql;
    }

    [Benchmark]
    public string SqlInterpolTemplate_PostgreSql()
    {
        var db = SqlBuilder.PostgreSql();
        var p = db.AddEntity<Product>();
        db.Append(_filteredSelectTemplate, p, new { CategoryId = _categoryId, MinPrice = _minPrice, IsActive = _isActive });
        return db.Build().Sql;
    }

    // -------------------------------------------------------------------------
    // Scenario 2: IN (...) clause — collection parameter expansion
    // -------------------------------------------------------------------------

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

    /// <summary>
    /// SqlKata: Uses .WhereIn() to expand collections dynamically.
    /// </summary>
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
        var p = db.AddEntity<Product>();
        db.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]} FROM {p} WHERE {p[x => x.CategoryId]} = {_categoryId} AND {p[x => x.Id]} IN ({_filterIds})");
        return db.Build().Sql;
    }

    // -------------------------------------------------------------------------
    // Scenario 3: Aliased multi-table JOIN — alias resolution
    // -------------------------------------------------------------------------

    [Benchmark]
    public string RawString_AliasedJoin() =>
        "SELECT o.\"Id\", o.\"CustomerId\", ol.\"Price\", ol.\"Quantity\" FROM \"dbo\".\"Orders\" AS o JOIN \"dbo\".\"OrderLines\" AS ol ON o.\"Id\" = ol.\"OrderId\" WHERE o.\"CustomerId\" = @p0";

    /// <summary>
    /// SqlKata: Aliases must be typed explicitly as magic strings in the From/Join definitions 
    /// and prefixed manually in the Select statements.
    /// </summary>
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
        var o = db.AddEntity<Order>(alias: "o");
        var ol = db.AddEntity<OrderLine>(alias: "ol");
        db.Append($"SELECT {o[x => x.Id]}, {o[x => x.CustomerId]}, {ol[x => x.Price]}, {ol[x => x.Quantity]} FROM {o} JOIN {ol} ON {o[x => x.Id]} = {ol[x => x.OrderId]} WHERE {o[x => x.CustomerId]} = {_customerId}");
        return db.Build().Sql;
    }

    [Benchmark]
    public string SqlInterpolTemplate_AliasedJoin()
    {
        var db = SqlBuilder.PostgreSql();
        var o = db.AddEntity<Order>(alias: "o");
        var ol = db.AddEntity<OrderLine>(alias: "ol");
        db.Append(_aliasedJoinTemplate, o, ol, new { CustomerId = _customerId });
        return db.Build().Sql;
    }
}