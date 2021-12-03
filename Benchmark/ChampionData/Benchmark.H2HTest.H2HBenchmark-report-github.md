``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1387 (21H2)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]    : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  MediumRun : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                        Method |        Mean |     Error |    StdDev |      Median |  Gen 0 | Allocated |
|------------------------------ |------------:|----------:|----------:|------------:|-------:|----------:|
|             SerializeProtoBuf |   393.20 ns |  3.185 ns |  4.568 ns |   393.35 ns | 0.0973 |     408 B |
|          SerializeMessagePack |   162.34 ns |  1.849 ns |  2.591 ns |   160.30 ns | 0.0134 |      56 B |
|             SerializeTinyhand |    94.34 ns |  1.066 ns |  1.529 ns |    94.39 ns | 0.0134 |      56 B |
|         SerializeTinyhandUtf8 |   643.67 ns |  0.860 ns |  1.260 ns |   643.66 ns | 0.0553 |     232 B |
|           DeserializeProtoBuf |   677.01 ns |  3.805 ns |  5.457 ns |   677.64 ns | 0.0763 |     320 B |
|        DeserializeMessagePack |   295.01 ns |  1.978 ns |  2.707 ns |   293.41 ns | 0.0668 |     280 B |
|           DeserializeTinyhand |   239.52 ns |  1.405 ns |  2.015 ns |   239.63 ns | 0.0668 |     280 B |
|       DeserializeTinyhandUtf8 | 1,421.10 ns | 15.022 ns | 21.059 ns | 1,415.66 ns | 0.1507 |     632 B |
|    SerializeMessagePackString |   183.26 ns |  8.583 ns | 12.309 ns |   172.45 ns | 0.0153 |      64 B |
|       SerializeTinyhandString |   128.80 ns |  0.727 ns |  1.043 ns |   128.82 ns | 0.0153 |      64 B |
|   SerializeTinyhandStringUtf8 | 1,005.17 ns |  1.242 ns |  1.700 ns | 1,005.28 ns | 0.0954 |     400 B |
|       SerializeJsonStringUtf8 |   508.77 ns |  1.679 ns |  2.242 ns |   507.83 ns | 0.1392 |     584 B |
|  DeserializeMessagePackString |   305.18 ns |  1.600 ns |  2.136 ns |   306.67 ns | 0.0668 |     280 B |
|     DeserializeTinyhandString |   281.50 ns |  1.696 ns |  2.378 ns |   280.02 ns | 0.0744 |     312 B |
| DeserializeTinyhandStringUtf8 | 1,594.04 ns |  2.790 ns |  4.002 ns | 1,592.91 ns | 0.1984 |     832 B |
|     DeserializeJsonStringUtf8 | 1,143.17 ns |  4.173 ns |  5.985 ns | 1,146.96 ns | 0.2155 |     904 B |
