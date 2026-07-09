```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8655)
Unknown processor
.NET SDK 10.0.301
  [Host]     : .NET 8.0.28 (8.0.2826.26413), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.28 (8.0.2826.26413), X64 RyuJIT AVX2


```
| Method            | Mean          | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |--------------:|-----------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| GenericLookup     |     0.5988 ns |  0.0223 ns |  0.0209 ns |     1.00 |    0.05 |      - |         - |          NA |
| RuntimeLookup     |     7.2094 ns |  0.0687 ns |  0.0609 ns |    12.05 |    0.42 |      - |         - |          NA |
| AddEntityAndBuild | 3,657.2224 ns | 28.2117 ns | 25.0090 ns | 6,114.05 |  210.24 | 0.3471 |    6568 B |          NA |
