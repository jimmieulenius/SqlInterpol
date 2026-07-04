using BenchmarkDotNet.Attributes;
using SqlInterpol.Benchmarks.Models;

namespace SqlInterpol.Benchmarks;

[MemoryDiagnoser]
public class EntityMetadataBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        _ = SqlMetadataRegistry.GetMetadata<Product>();
        _ = SqlMetadataRegistry.GetMetadata<Order>();
        _ = SqlMetadataRegistry.GetMetadata(typeof(Product));
        _ = SqlMetadataRegistry.GetMetadata(typeof(Order));
    }

    [Benchmark(Baseline = true)]
    public SqlEntityMetadata GenericLookup() => SqlMetadataRegistry.GetMetadata<Product>();

    [Benchmark]
    public SqlEntityMetadata RuntimeLookup() => SqlMetadataRegistry.GetMetadata(typeof(Product));

    [Benchmark]
    public string AddEntityAndBuild()
    {
        var db = SqlBuilder.PostgreSql();
        db.Entity<Product>(out var p);
        db.Append($"SELECT {p.Id} FROM {p} WHERE {p.CategoryId} = {1}");
        return db.Build().Sql;
    }
}