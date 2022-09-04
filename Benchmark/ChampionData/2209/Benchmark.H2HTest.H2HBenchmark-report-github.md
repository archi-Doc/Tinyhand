``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1949 (21H2)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.400
  [Host]    : .NET 6.0.8 (6.0.822.36306), X64 RyuJIT
  MediumRun : .NET 6.0.8 (6.0.822.36306), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                        Method |        Mean |     Error |    StdDev |      Median |  Gen 0 | Allocated |
|------------------------------ |------------:|----------:|----------:|------------:|-------:|----------:|
|             SerializeProtoBuf |   391.61 ns |  5.615 ns |  7.495 ns |   388.60 ns | 0.0973 |     408 B |
|          SerializeMessagePack |   175.70 ns |  3.855 ns |  5.405 ns |   180.58 ns | 0.0134 |      56 B |
|             SerializeTinyhand |    88.09 ns |  0.549 ns |  0.822 ns |    88.11 ns | 0.0134 |      56 B |
|         SerializeTinyhandUtf8 |   706.40 ns |  0.729 ns |  1.045 ns |   706.66 ns | 0.0534 |     224 B |
|           DeserializeProtoBuf |   692.52 ns |  3.091 ns |  4.126 ns |   693.15 ns | 0.0763 |     320 B |
|        DeserializeMessagePack |   296.82 ns |  0.893 ns |  1.161 ns |   296.81 ns | 0.0668 |     280 B |
|           DeserializeTinyhand |   223.01 ns |  1.307 ns |  1.874 ns |   222.08 ns | 0.0668 |     280 B |
|       DeserializeTinyhandUtf8 | 1,491.16 ns | 20.290 ns | 27.773 ns | 1,498.41 ns | 0.1431 |     600 B |
|    SerializeMessagePackString |   173.73 ns |  0.655 ns |  0.918 ns |   173.07 ns | 0.0153 |      64 B |
|       SerializeTinyhandString |   126.52 ns |  0.766 ns |  1.122 ns |   125.80 ns | 0.0153 |      64 B |
|   SerializeTinyhandStringUtf8 | 1,012.20 ns |  2.023 ns |  2.769 ns | 1,012.34 ns | 0.0916 |     384 B |
|       SerializeJsonStringUtf8 |   505.10 ns |  1.642 ns |  2.302 ns |   504.10 ns | 0.1392 |     584 B |
|  DeserializeMessagePackString |   304.14 ns |  2.680 ns |  3.669 ns |   301.82 ns | 0.0668 |     280 B |
|     DeserializeTinyhandString |   260.24 ns |  1.113 ns |  1.523 ns |   259.31 ns | 0.0744 |     312 B |
| DeserializeTinyhandStringUtf8 | 1,582.17 ns |  2.487 ns |  3.567 ns | 1,582.36 ns | 0.1526 |     640 B |
|     DeserializeJsonStringUtf8 | 1,145.40 ns |  2.594 ns |  3.802 ns | 1,144.11 ns | 0.2155 |     904 B |
