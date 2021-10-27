``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100-rc.2.21505.57
  [Host]    : .NET 6.0.0 (6.0.21.48005), X64 RyuJIT
  MediumRun : .NET 6.0.0 (6.0.21.48005), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                        Method |       Mean |    Error |   StdDev |     Median |  Gen 0 | Allocated |
|------------------------------ |-----------:|---------:|---------:|-----------:|-------:|----------:|
|             SerializeProtoBuf |   404.5 ns |  2.32 ns |  3.17 ns |   402.0 ns | 0.0973 |     408 B |
|          SerializeMessagePack |   174.8 ns |  1.05 ns |  1.54 ns |   174.0 ns | 0.0134 |      56 B |
|             SerializeTinyhand |   104.6 ns |  1.99 ns |  2.98 ns |   104.0 ns | 0.0134 |      56 B |
|         SerializeTinyhandUtf8 |   655.7 ns |  1.00 ns |  1.46 ns |   655.8 ns | 0.0553 |     232 B |
|           DeserializeProtoBuf |   695.3 ns |  9.37 ns | 13.44 ns |   705.1 ns | 0.0763 |     320 B |
|        DeserializeMessagePack |   300.8 ns |  3.29 ns |  4.51 ns |   301.4 ns | 0.0668 |     280 B |
|           DeserializeTinyhand |   240.4 ns |  2.58 ns |  3.70 ns |   243.3 ns | 0.0668 |     280 B |
|       DeserializeTinyhandUtf8 | 1,421.7 ns |  1.69 ns |  2.42 ns | 1,421.6 ns | 0.1507 |     632 B |
|    SerializeMessagePackString |   177.2 ns |  1.45 ns |  2.03 ns |   175.8 ns | 0.0153 |      64 B |
|       SerializeTinyhandString |   133.3 ns |  0.95 ns |  1.27 ns |   133.1 ns | 0.0153 |      64 B |
|   SerializeTinyhandStringUtf8 | 1,001.6 ns |  2.76 ns |  3.96 ns | 1,000.5 ns | 0.0954 |     400 B |
|       SerializeJsonStringUtf8 |   515.5 ns |  3.96 ns |  5.68 ns |   519.9 ns | 0.1392 |     584 B |
|  DeserializeMessagePackString |   302.0 ns |  0.46 ns |  0.67 ns |   302.1 ns | 0.0668 |     280 B |
|     DeserializeTinyhandString |   274.9 ns |  0.83 ns |  1.17 ns |   275.5 ns | 0.0744 |     312 B |
| DeserializeTinyhandStringUtf8 | 1,650.4 ns | 10.49 ns | 15.05 ns | 1,660.9 ns | 0.1984 |     832 B |
|     DeserializeJsonStringUtf8 | 1,131.1 ns |  1.54 ns |  2.26 ns | 1,131.9 ns | 0.2155 |     904 B |
