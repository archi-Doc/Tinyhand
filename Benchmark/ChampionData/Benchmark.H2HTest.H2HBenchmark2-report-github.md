``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.103
  [Host]    : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  MediumRun : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                        Method |       Mean |    Error |   StdDev |     Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |-----------:|---------:|---------:|-----------:|-------:|------:|------:|----------:|
|             SerializeProtoBuf |   453.5 ns |  1.99 ns |  2.92 ns |   454.3 ns | 0.0973 |     - |     - |     408 B |
|          SerializeMessagePack |   167.3 ns |  2.61 ns |  3.67 ns |   165.1 ns | 0.0134 |     - |     - |      56 B |
|             SerializeTinyhand |   135.6 ns |  0.41 ns |  0.58 ns |   135.9 ns | 0.0134 |     - |     - |      56 B |
|         SerializeTinyhandUtf8 |   825.1 ns |  1.45 ns |  2.08 ns |   825.1 ns | 0.0553 |     - |     - |     232 B |
|           DeserializeProtoBuf |   743.1 ns |  9.65 ns | 14.44 ns |   744.7 ns | 0.0763 |     - |     - |     320 B |
|        DeserializeMessagePack |   300.3 ns |  1.47 ns |  2.20 ns |   300.3 ns | 0.0668 |     - |     - |     280 B |
|           DeserializeTinyhand |   277.5 ns |  1.78 ns |  2.38 ns |   279.0 ns | 0.0668 |     - |     - |     280 B |
|       DeserializeTinyhandUtf8 | 1,548.0 ns |  2.56 ns |  3.76 ns | 1,547.5 ns | 0.1507 |     - |     - |     632 B |
|    SerializeMessagePackString |   175.8 ns |  0.92 ns |  1.32 ns |   176.9 ns | 0.0153 |     - |     - |      64 B |
|       SerializeTinyhandString |   145.7 ns |  1.13 ns |  1.69 ns |   145.8 ns | 0.0153 |     - |     - |      64 B |
|   SerializeTinyhandStringUtf8 | 1,119.9 ns |  1.08 ns |  1.52 ns | 1,119.6 ns | 0.0954 |     - |     - |     400 B |
|       SerializeJsonStringUtf8 |   554.6 ns |  4.07 ns |  5.58 ns |   556.4 ns | 0.1316 |     - |     - |     552 B |
|  DeserializeMessagePackString |   321.8 ns |  0.55 ns |  0.77 ns |   321.8 ns | 0.0668 |     - |     - |     280 B |
|     DeserializeTinyhandString |   310.6 ns |  1.25 ns |  1.79 ns |   310.5 ns | 0.0744 |     - |     - |     312 B |
| DeserializeTinyhandStringUtf8 | 1,727.1 ns |  1.98 ns |  2.84 ns | 1,726.9 ns | 0.1984 |     - |     - |     832 B |
|     DeserializeJsonStringUtf8 | 1,395.5 ns | 15.51 ns | 22.25 ns | 1,411.4 ns | 0.2232 |     - |     - |     936 B |
