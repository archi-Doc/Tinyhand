``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]    : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  MediumRun : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                       Method |     Mean |   Error |  StdDev |   Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |---------:|--------:|--------:|---------:|-------:|------:|------:|----------:|
|            SerializeProtoBuf | 449.3 ns | 4.31 ns | 6.04 ns | 452.5 ns | 0.0973 |     - |     - |     408 B |
|         SerializeMessagePack | 163.9 ns | 1.33 ns | 1.90 ns | 163.2 ns | 0.0134 |     - |     - |      56 B |
|            SerializeTinyhand | 140.4 ns | 3.32 ns | 4.97 ns | 141.0 ns | 0.0134 |     - |     - |      56 B |
|          DeserializeProtoBuf | 737.6 ns | 2.45 ns | 3.66 ns | 737.1 ns | 0.0763 |     - |     - |     320 B |
|       DeserializeMessagePack | 306.6 ns | 0.66 ns | 0.93 ns | 306.7 ns | 0.0668 |     - |     - |     280 B |
|          DeserializeTinyhand | 280.4 ns | 2.22 ns | 3.19 ns | 280.7 ns | 0.0668 |     - |     - |     280 B |
|   SerializeMessagePackString | 179.1 ns | 2.64 ns | 3.79 ns | 179.3 ns | 0.0153 |     - |     - |      64 B |
|      SerializeTinyhandString | 143.8 ns | 2.18 ns | 3.19 ns | 142.1 ns | 0.0153 |     - |     - |      64 B |
| DeserializeMessagePackString | 320.1 ns | 1.12 ns | 1.60 ns | 319.7 ns | 0.0668 |     - |     - |     280 B |
|    DeserializeTinyhandString | 311.3 ns | 1.31 ns | 1.84 ns | 310.8 ns | 0.0744 |     - |     - |     312 B |
