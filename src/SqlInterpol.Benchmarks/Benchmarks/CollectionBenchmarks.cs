using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

/// <summary>
/// Measures collection fragment rendering for IN (...) clauses across varying list sizes.
/// Each item in the collection becomes a separate SQL parameter, so this exercises both
/// the parameter-generation loop and the final SQL rendering.
/// </summary>
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
        var o = db.AddEntity<Order>();
        db.Append($"SELECT {o[x => x.Id]}, {o[x => x.Total]} FROM {o} WHERE {o[x => x.Id]} IN ({_ids})");
        return db.Build().Sql;
    }

    [Benchmark]
    public string InClause_SqlServer()
    {
        var db = SqlBuilder.SqlServer();
        var o = db.AddEntity<Order>();
        db.Append($"SELECT {o[x => x.Id]}, {o[x => x.Total]} FROM {o} WHERE {o[x => x.Id]} IN ({_ids})");
        return db.Build().Sql;
    }

    [Benchmark]
    public string InClause_MySql()
    {
        var db = SqlBuilder.MySql();
        var o = db.AddEntity<Order>();
        db.Append($"SELECT {o[x => x.Id]}, {o[x => x.Total]} FROM {o} WHERE {o[x => x.Id]} IN ({_ids})");
        return db.Build().Sql;
    }
}
