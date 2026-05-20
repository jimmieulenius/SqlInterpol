using BenchmarkDotNet.Attributes;
using DapperSB = Dapper.SqlBuilder;
using SqlInterpol.Benchmarks.Models;
using System.Text;

namespace SqlInterpol.Benchmarks;

/// <summary>
/// Compares SqlInterpol against raw strings and Dapper.SqlBuilder across three scenarios:
/// 1. Simple filtered SELECT (baseline)
/// 2. IN (...) clause with a variable-size collection (parameter expansion)
/// 3. Aliased multi-table JOIN (alias resolution)
/// Dialect rewriting (e.g. FOR UPDATE → WITH(UPDLOCK), EXCEPT → MINUS) has no raw equivalent
/// and is covered separately in MultiDialectBenchmarks.
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

    // -------------------------------------------------------------------------
    // Scenario 1: Simple filtered SELECT
    // -------------------------------------------------------------------------

    /// <summary>
    /// Hand-written SQL string constant — absolute minimum overhead floor.
    /// No type safety, no dialect switching, identifiers and schema hardcoded.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string RawString() =>
        "SELECT \"Id\", \"PROD_NAME\", \"Price\" FROM \"dbo\".\"Products\" WHERE \"CategoryId\" = @p0 AND \"Price\" >= @p1 AND \"IsActive\" = @p2";

    /// <summary>
    /// Dapper.SqlBuilder — clause-based template substitution.
    /// No identifier quoting, no dialect support, parameters are caller-managed.
    /// </summary>
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
    /// SqlInterpol (PostgreSQL) — typed entity, auto identifier quoting, auto parameterization.
    /// </summary>
    [Benchmark]
    public string SqlInterpol_PostgreSql()
    {
        var db = SqlBuilder.PostgreSql();
        var p = db.AddEntity<Product>();
        db.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]}, {p[x => x.Price]} FROM {p} WHERE {p[x => x.CategoryId]} = {_categoryId} AND {p[x => x.Price]} >= {_minPrice} AND {p[x => x.IsActive]} = {_isActive}");
        return db.Build().Sql;
    }

    /// <summary>
    /// SqlInterpol (SQL Server) — same code, different dialect, no changes needed.
    /// </summary>
    [Benchmark]
    public string SqlInterpol_SqlServer()
    {
        var db = SqlBuilder.SqlServer();
        var p = db.AddEntity<Product>();
        db.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]}, {p[x => x.Price]} FROM {p} WHERE {p[x => x.CategoryId]} = {_categoryId} AND {p[x => x.Price]} >= {_minPrice} AND {p[x => x.IsActive]} = {_isActive}");
        return db.Build().Sql;
    }

    // -------------------------------------------------------------------------
    // Scenario 2: IN (...) clause — collection parameter expansion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Raw: must manually loop to build @p0,@p1,...,@pN placeholders.
    /// </summary>
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
    /// SqlInterpol: pass the array directly — parameters are expanded and registered automatically.
    /// </summary>
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

    /// <summary>
    /// Raw: alias prefix, quoted identifiers, and schema all hardcoded.
    /// Any rename requires updating every SQL string manually.
    /// </summary>
    [Benchmark]
    public string RawString_AliasedJoin() =>
        "SELECT o.\"Id\", o.\"CustomerId\", ol.\"Price\", ol.\"Quantity\" FROM \"dbo\".\"Orders\" AS o JOIN \"dbo\".\"OrderLines\" AS ol ON o.\"Id\" = ol.\"OrderId\" WHERE o.\"CustomerId\" = @p0";

    /// <summary>
    /// SqlInterpol: aliases declared once on the entity; all column refs resolve automatically.
    /// Rename a property → the SQL updates everywhere.
    /// </summary>
    [Benchmark]
    public string SqlInterpol_AliasedJoin()
    {
        var db = SqlBuilder.PostgreSql();
        var o = db.AddEntity<Order>(alias: "o");
        var ol = db.AddEntity<OrderLine>(alias: "ol");
        db.Append($"SELECT {o[x => x.Id]}, {o[x => x.CustomerId]}, {ol[x => x.Price]}, {ol[x => x.Quantity]} FROM {o} JOIN {ol} ON {o[x => x.Id]} = {ol[x => x.OrderId]} WHERE {o[x => x.CustomerId]} = {_customerId}");
        return db.Build().Sql;
    }
}
