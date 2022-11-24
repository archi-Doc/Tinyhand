``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.2311)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]  : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  LongRun : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=LongRun  IterationCount=100  LaunchCount=3  
WarmupCount=15  

```
|                       Method |        Mean |    Error |   StdDev |      Median |   Gen0 | Allocated |
|----------------------------- |------------:|---------:|---------:|------------:|-------:|----------:|
|            SerializeProtoBuf |   401.90 ns | 1.847 ns | 9.089 ns |   397.35 ns | 0.0973 |     408 B |
|         SerializeMessagePack |   170.99 ns | 0.365 ns | 1.865 ns |   171.32 ns | 0.0134 |      56 B |
|          SerializeMemoryPack |   112.48 ns | 0.996 ns | 5.054 ns |   110.51 ns | 0.0229 |      96 B |
|            SerializeTinyhand |    80.51 ns | 0.104 ns | 0.524 ns |    80.72 ns | 0.0134 |      56 B |
|          DeserializeProtoBuf |   689.49 ns | 1.435 ns | 7.297 ns |   686.76 ns | 0.0763 |     320 B |
|       DeserializeMessagePack |   288.63 ns | 0.306 ns | 1.556 ns |   288.71 ns | 0.0668 |     280 B |
|        DeserializeMemoryPack |   124.30 ns | 0.367 ns | 1.895 ns |   123.35 ns | 0.0668 |     280 B |
|          DeserializeTinyhand |   145.02 ns | 1.230 ns | 6.186 ns |   149.17 ns | 0.0668 |     280 B |
|   SerializeMessagePackString |   178.74 ns | 0.286 ns | 1.446 ns |   178.45 ns | 0.0153 |      64 B |
|      SerializeTinyhandString |   128.12 ns | 0.196 ns | 0.986 ns |   127.69 ns | 0.0153 |      64 B |
|        SerializeTinyhandUtf8 |   650.83 ns | 0.720 ns | 3.589 ns |   650.99 ns | 0.0916 |     384 B |
|            SerializeJsonUtf8 |   495.27 ns | 1.119 ns | 5.672 ns |   495.46 ns | 0.0954 |     400 B |
| DeserializeMessagePackString |   286.31 ns | 1.621 ns | 8.287 ns |   281.30 ns | 0.0668 |     280 B |
|    DeserializeTinyhandString |   175.70 ns | 0.531 ns | 2.624 ns |   175.77 ns | 0.0744 |     312 B |
|      DeserializeTinyhandUtf8 | 1,319.04 ns | 1.088 ns | 5.512 ns | 1,321.51 ns | 0.1526 |     640 B |
|          DeserializeJsonUtf8 | 1,045.53 ns | 1.286 ns | 6.574 ns | 1,047.47 ns | 0.2232 |     936 B |
