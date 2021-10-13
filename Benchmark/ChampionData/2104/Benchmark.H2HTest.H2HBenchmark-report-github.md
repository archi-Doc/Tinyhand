``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.201
  [Host]    : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  MediumRun : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                        Method |       Mean |    Error |   StdDev |     Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |-----------:|---------:|---------:|-----------:|-------:|------:|------:|----------:|
|             SerializeProtoBuf |   478.1 ns |  2.36 ns |  3.53 ns |   478.1 ns | 0.0973 |     - |     - |     408 B |
|          SerializeMessagePack |   170.1 ns |  0.39 ns |  0.57 ns |   170.4 ns | 0.0134 |     - |     - |      56 B |
|             SerializeTinyhand |   138.6 ns |  2.73 ns |  4.01 ns |   135.4 ns | 0.0134 |     - |     - |      56 B |
|         SerializeTinyhandUtf8 |   849.7 ns |  1.35 ns |  1.75 ns |   849.5 ns | 0.0553 |     - |     - |     232 B |
|           DeserializeProtoBuf |   789.7 ns |  2.58 ns |  3.86 ns |   790.5 ns | 0.0763 |     - |     - |     320 B |
|        DeserializeMessagePack |   304.1 ns |  0.31 ns |  0.44 ns |   304.2 ns | 0.0668 |     - |     - |     280 B |
|           DeserializeTinyhand |   278.2 ns |  0.40 ns |  0.56 ns |   278.0 ns | 0.0668 |     - |     - |     280 B |
|       DeserializeTinyhandUtf8 | 1,576.5 ns | 10.35 ns | 15.17 ns | 1,574.5 ns | 0.1507 |     - |     - |     632 B |
|    SerializeMessagePackString |   186.1 ns |  0.94 ns |  1.38 ns |   185.1 ns | 0.0153 |     - |     - |      64 B |
|       SerializeTinyhandString |   148.2 ns |  3.12 ns |  4.27 ns |   148.2 ns | 0.0153 |     - |     - |      64 B |
|   SerializeTinyhandStringUtf8 | 1,190.0 ns |  3.58 ns |  5.13 ns | 1,190.2 ns | 0.0954 |     - |     - |     400 B |
|       SerializeJsonStringUtf8 |   560.0 ns |  1.56 ns |  2.24 ns |   560.1 ns | 0.1316 |     - |     - |     552 B |
|  DeserializeMessagePackString |   333.0 ns |  0.68 ns |  0.96 ns |   333.4 ns | 0.0668 |     - |     - |     280 B |
|     DeserializeTinyhandString |   321.7 ns |  2.15 ns |  3.08 ns |   321.8 ns | 0.0744 |     - |     - |     312 B |
| DeserializeTinyhandStringUtf8 | 1,794.9 ns |  3.55 ns |  5.21 ns | 1,794.4 ns | 0.1984 |     - |     - |     832 B |
|     DeserializeJsonStringUtf8 | 1,341.8 ns |  1.32 ns |  1.89 ns | 1,341.3 ns | 0.2232 |     - |     - |     936 B |
