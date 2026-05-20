using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

/// <summary>
/// Isolates entity metadata resolution costs:
/// the static CLR generic class path vs the ConcurrentDictionary runtime path.
/// Also measures the per-query AddEntity overhead (metadata lookup + entity instantiation).
/// </summary>
[MemoryDiagnoser]
public class EntityMetadataBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Warm both cache paths before measuring
        _ = SqlMetadataRegistry.GetMetadata<Product>();
        _ = SqlMetadataRegistry.GetMetadata<Order>();
        _ = SqlMetadataRegistry.GetMetadata(typeof(Product));
        _ = SqlMetadataRegistry.GetMetadata(typeof(Order));
    }

    /// <summary>
    /// Generic path: <c>Cache&lt;T&gt;.Metadata</c> is a CLR static field — no dictionary lookup.
    /// </summary>
    [Benchmark(Baseline = true)]
    public SqlEntityMetadata GenericLookup() => SqlMetadataRegistry.GetMetadata<Product>();

    /// <summary>
    /// Runtime (non-generic) path: <c>ConcurrentDictionary.GetOrAdd</c> on a warm cache.
    /// </summary>
    [Benchmark]
    public SqlEntityMetadata RuntimeLookup() => SqlMetadataRegistry.GetMetadata(typeof(Product));

    /// <summary>
    /// Full <c>AddEntity&lt;T&gt;()</c> round-trip on a fresh builder:
    /// metadata lookup + ISqlEntity instantiation + builder registration.
    /// </summary>
    [Benchmark]
    public string AddEntityAndBuild()
    {
        var db = SqlBuilder.PostgreSql();
        var p = db.AddEntity<Product>();
        db.Append($"SELECT {p[x => x.Id]} FROM {p} WHERE {p[x => x.CategoryId]} = {1}");
        return db.Build().Sql;
    }
}
