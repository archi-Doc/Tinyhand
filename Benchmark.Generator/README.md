# Generator performance

Measures the source generators themselves, including `StaticRegistrationGenerator` by default.
This does not measure serialization speed or total project build time.

```powershell
dotnet build Benchmark.Generator/Benchmark.Generator.csproj -c Release
$env:DOTNET_TieredCompilation = '0'
dotnet Benchmark.Generator/bin/Release/net10.0/Benchmark.Generator.dll --iterations 25 --output artifacts/generator-after.json
```

For a before/after comparison, save the original `TinyhandGenerator.dll` before rebuilding,
then pass its absolute or repository-relative path with `--generator`. The runner uses an
isolated assembly load context so an older DLL with the same assembly identity is actually
loaded. The loaded path is printed at startup. Use `--main-only` to measure only
`TinyhandGenerator`, excluding static registration.

```powershell
# Before modifying the generator:
New-Item -ItemType Directory -Force artifacts/generator-baseline
Copy-Item TinyhandGenerator/bin/Release/netstandard2.0/TinyhandGenerator.dll artifacts/generator-baseline/

# After rebuilding the benchmark and generator:
dotnet Benchmark.Generator/bin/Release/net10.0/Benchmark.Generator.dll --iterations 25 --generator artifacts/generator-baseline/TinyhandGenerator.dll --output artifacts/generator-before.json
dotnet Benchmark.Generator/bin/Release/net10.0/Benchmark.Generator.dll --iterations 25 --output artifacts/generator-after.json
```

Each case uses ten source trees. `Models` contains 100 classes with six scalar properties;
`Collections` replaces two properties with enum arrays and lists. `GenericNoise` contains
ten generic Tinyhand types and 1,000 methods using framework generic collections and tuples.

Each sample creates a fresh generator driver. Cold samples also create a fresh compilation;
warm samples reuse a compilation with cached symbols. Neither measures an unchanged incremental
driver cache hit. Source parsing, compilation construction, output validation and explicit GCs
are outside the measured interval. Five warmups precede measurement. The report contains the
median elapsed time and median process-wide allocation delta from `GC.GetTotalAllocatedBytes`,
including generator worker threads. Concurrent compilation remains enabled.

Every sample must produce the same generated source and diagnostics. The combined generators'
output is also compiled once per scenario and must have no errors. `OutputHash` includes the
ordered generated file names, source text and diagnostics; compare it between baseline and candidate
runs to check output compatibility. Time and allocation values are scenario-dependent and are
not test thresholds.

## Measurements on 2026-09-28

Windows x64, .NET SDK 10.0.401 / runtime 10.0.12, Release, tiered compilation disabled,
25 samples per case. Baseline: `3a25c4cead287ad5bf0593d80604eeb72933f9c6`.
Both generators enabled. All six output hashes matched the baseline.

| Case | Mode | Before ms | After ms | Before MiB | After MiB |
|---|---|---:|---:|---:|---:|
| Models | Cold | 17.12 | 15.21 | 15.24 | 12.68 |
| Models | Warm | 7.20 | 6.24 | 11.30 | 8.88 |
| Collections | Cold | 26.27 | 16.97 | 19.21 | 15.94 |
| Collections | Warm | 8.52 | 7.36 | 13.62 | 10.64 |
| GenericNoise | Cold | 61.07 | 61.44 | 53.54 | 48.46 |
| GenericNoise | Warm | 57.36 | 56.03 | 48.69 | 43.62 |

Models and collections improved by 11–35% in elapsed time and 17–22% in allocation.
Generic-heavy input allocated 9–10% less; its elapsed time was essentially unchanged.
