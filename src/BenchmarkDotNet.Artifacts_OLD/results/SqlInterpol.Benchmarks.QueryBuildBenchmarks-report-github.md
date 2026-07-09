```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8655)
Unknown processor
.NET SDK 10.0.301
  [Host]     : .NET 8.0.28 (8.0.2826.26413), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.28 (8.0.2826.26413), X64 RyuJIT AVX2


```
| Method                  | Mean        | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| SimpleSelect            |  4,701.1 ns | 46.06 ns | 35.96 ns |  1.00 |    0.01 | 0.4349 |      - |   8.07 KB |        1.00 |
| Template_SimpleSelect   |    700.0 ns |  5.00 ns |  4.43 ns |  0.15 |    0.00 | 0.1860 | 0.0010 |   3.43 KB |        0.42 |
| FilteredSelect          |  7,457.1 ns | 57.08 ns | 50.60 ns |  1.59 |    0.02 | 0.6027 | 0.0076 |  11.13 KB |        1.38 |
| Template_FilteredSelect |    855.6 ns |  3.30 ns |  2.75 ns |  0.18 |    0.00 | 0.2108 | 0.0010 |   3.88 KB |        0.48 |
| JoinQuery               |  8,173.1 ns | 48.94 ns | 43.39 ns |  1.74 |    0.02 | 0.6561 |      - |  12.13 KB |        1.50 |
| Template_JoinQuery      |    713.7 ns |  9.16 ns |  8.56 ns |  0.15 |    0.00 | 0.1926 | 0.0010 |   3.55 KB |        0.44 |
| ComplexJoinWithPaging   | 18,115.2 ns | 34.60 ns | 32.37 ns |  3.85 |    0.03 | 1.1597 | 0.0305 |  21.36 KB |        2.65 |
