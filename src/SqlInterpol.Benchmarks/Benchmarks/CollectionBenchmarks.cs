using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

[MemoryDiagnoser]
public class CollectionBenchmarks
{
    [Params(5, 25, 100)]
    public int Count { get; set; }

    private int[] _ids = [];

    [GlobalSetup]
    public void Setup() => _ids = Enumerable.Range(1, Count).ToArray();

    [Benchmark(Baseline = true)]
    public string InClause_PostgreSql()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Order>(out var o);
        db.Append($"SELECT {o.Id}, {o.Total} FROM {o} WHERE {o.Id} IN ({_ids})");
        return db.Build().Sql;
    }

    [Benchmark]
    public string InClause_SqlServer()
    {
        var db = SqlBuilder.SqlServer();
        db.Entity<Order>(out var o);
        db.Append($"SELECT {o.Id}, {o.Total} FROM {o} WHERE {o.Id} IN ({_ids})");
        return db.Build().Sql;
    }

    [Benchmark]
    public string InClause_MySql()
    {
        var db = SqlBuilder.MySql();
        db.Entity<Order>(out var o);
        db.Append($"SELECT {o.Id}, {o.Total} FROM {o} WHERE {o.Id} IN ({_ids})");
        return db.Build().Sql;
    }
}