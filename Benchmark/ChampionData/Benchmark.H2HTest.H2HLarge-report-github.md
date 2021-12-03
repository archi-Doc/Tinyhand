``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1387 (21H2)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]    : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  MediumRun : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                    Method |      Mean |     Error |    StdDev |    Median |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|-------------------------- |----------:|----------:|----------:|----------:|---------:|---------:|---------:|----------:|
|      SerializeMessagePack |  7.669 ms | 0.1969 ms | 0.2947 ms |  7.418 ms |  93.7500 |  93.7500 |  93.7500 |      6 MB |
|         SerializeTinyhand |  7.364 ms | 0.0056 ms | 0.0078 ms |  7.362 ms | 125.0000 | 125.0000 | 125.0000 |      6 MB |
|    DeserializeMessagePack | 12.611 ms | 0.0421 ms | 0.0562 ms | 12.619 ms | 171.8750 | 156.2500 |  93.7500 |      5 MB |
|       DeserializeTinyhand | 11.919 ms | 0.0572 ms | 0.0783 ms | 11.928 ms | 171.8750 | 156.2500 |  93.7500 |      5 MB |
|   SerializeMessagePackLz4 | 22.511 ms | 0.0090 ms | 0.0123 ms | 22.507 ms |  62.5000 |  62.5000 |  62.5000 |      5 MB |
|      SerializeTinyhandLz4 | 22.210 ms | 0.0103 ms | 0.0147 ms | 22.204 ms |  62.5000 |  62.5000 |  62.5000 |      5 MB |
| DeserializeMessagePackLz4 | 18.245 ms | 0.0984 ms | 0.1473 ms | 18.279 ms | 125.0000 |  93.7500 |  62.5000 |      5 MB |
|    DeserializeTinyhandLz4 | 17.102 ms | 0.0570 ms | 0.0817 ms | 17.122 ms | 125.0000 |  93.7500 |  62.5000 |      5 MB |
