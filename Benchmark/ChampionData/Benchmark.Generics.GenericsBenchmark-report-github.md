``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.201
  [Host]    : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  MediumRun : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                               Method |      Mean |    Error |   StdDev |    Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------- |----------:|---------:|---------:|----------:|-------:|------:|------:|----------:|
|         Serialize_NormalInt_Tinyhand |  99.61 ns | 1.025 ns | 1.534 ns |  99.68 ns | 0.0210 |     - |     - |      88 B |
|         Serialize_CustomInt_Tinyhand | 132.57 ns | 0.324 ns | 0.465 ns | 132.42 ns | 0.0210 |     - |     - |      88 B |
|      Serialize_NormalInt_MessagePack | 140.11 ns | 1.585 ns | 2.222 ns | 138.52 ns | 0.0210 |     - |     - |      88 B |
|       Serialize_GenericsInt_Tinyhand |  99.18 ns | 0.165 ns | 0.231 ns |  99.14 ns | 0.0210 |     - |     - |      88 B |
|    Serialize_GenericsInt_MessagePack | 140.59 ns | 1.054 ns | 1.478 ns | 141.46 ns | 0.0210 |     - |     - |      88 B |
|      Serialize_NormalString_Tinyhand | 130.62 ns | 0.159 ns | 0.228 ns | 130.63 ns | 0.0229 |     - |     - |      96 B |
|   Serialize_NormalString_MessagePack | 155.96 ns | 2.213 ns | 3.244 ns | 158.50 ns | 0.0229 |     - |     - |      96 B |
|    Serialize_GenericsString_Tinyhand | 133.88 ns | 0.345 ns | 0.495 ns | 133.86 ns | 0.0229 |     - |     - |      96 B |
| Serialize_GenericsString_MessagePack | 157.12 ns | 1.950 ns | 2.858 ns | 159.15 ns | 0.0229 |     - |     - |      96 B |
|       Deserialize_NormalInt_Tinyhand | 200.50 ns | 0.598 ns | 0.818 ns | 200.36 ns | 0.0248 |     - |     - |     104 B |
|       Deserialize_CustomInt_Tinyhand | 235.30 ns | 0.989 ns | 1.386 ns | 234.96 ns | 0.0248 |     - |     - |     104 B |
|     Deserialize_GenericsInt_Tinyhand | 204.80 ns | 3.648 ns | 5.114 ns | 202.12 ns | 0.0248 |     - |     - |     104 B |
|    Deserialize_NormalString_Tinyhand | 259.58 ns | 3.532 ns | 5.065 ns | 263.07 ns | 0.0324 |     - |     - |     136 B |
|  Deserialize_GenericsString_Tinyhand | 252.82 ns | 1.247 ns | 1.788 ns | 252.07 ns | 0.0324 |     - |     - |     136 B |
