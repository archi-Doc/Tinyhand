# Tinyhand IO optimization — 2026-09-03

The changes preserve public API signatures and valid wire representations. They remove the integer T4 template, reduce allocations, improve the measured serialization paths, and reject malformed input consistently.

## Measurement

- Baseline commit: `0db87b2d322aa540efb04adbe8742ff999cf7710`.
- Benchmark: `Benchmark.Utf8Benchmark`, BenchmarkDotNet 0.15.8.
- Environment: Windows 11, Intel Core i9-13900K, .NET SDK 10.0.400, .NET runtime 10.0.11, x64 RyuJIT.
- Release build; unchanged MediumRun configuration: 2 launches, 10 warmups, 15 measured iterations per launch.
- Baseline and candidates ran sequentially; tests and builds for other candidates were not run alongside measured workloads.

| Method | Before (ns) | Final (ns) | Time reduction | Allocated per operation, before → final |
|---|---:|---:|---:|---:|
| SerializeTinyhand | 27.33 | 26.43 | 3.3% | 48 B → 48 B |
| DeserializeTinyhand | 50.26 | 42.64 | 15.2% | 256 B → 256 B |
| SerializeTinyhandUtf8 | 162.68 | 151.89 | 6.6% | 80 B → 80 B |
| DeserializeTinyhandUtf8 | 220.26 | 197.82 | 10.2% | 216 B → 216 B |

BenchmarkDotNet's reported error margins (before → final, ns): SerializeTinyhand 0.375 → 0.210; DeserializeTinyhand 1.279 → 1.617; SerializeTinyhandUtf8 2.293 → 0.318; DeserializeTinyhandUtf8 2.786 → 0.870.

Two methods added to the same benchmark compare the previous intermediate-string parsing approach with the new `TryReadStringConvertible` implementation. Both read the same nine-digit integer and use the same value-type parser.

| Parsing path | Mean | Error | Allocated per operation |
|---|---:|---:|---:|
| StringConvertibleViaString | 14.12 ns | 0.112 ns | 40 B |
| StringConvertibleViaSpan | 11.46 ns | 0.161 ns | 0 B |

The new parsing path is 18.8% faster in this case. Long inputs use ArrayPool; a cold pool or a pool miss can still allocate. Allocations made by a user-provided parser are outside the temporary-buffer optimization.

The four original benchmarks cover their existing sample objects, not every possible IO workload. Results are machine-specific, and small differences should be interpreted with the reported variation. No measured original benchmark regressed.

## Implementation

- Removed `TinyhandReader.Integer.tt` and its project generator metadata. Each integer reader is maintained directly in C#.
- Inlined fixint reads; a signed comparison recognizes both fixint ranges. Other integer formats use the eight consecutive codes in a dense switch, with checked narrowing conversions.
- Confirmed `ReadInt32Slow` emits an indexed jump table and `bswap` instructions on the measured x64 runtime. No delegate table or per-call allocation is used.
- Replaced byte-by-byte big-endian writes and custom 128-bit endian reversal with BinaryPrimitives.
- Replaced managed type/name lookup arrays with assembly-backed read-only byte data and constant-string dispatch. This removes the two managed array allocations at type initialization; steady-state allocation columns do not measure that startup saving.
- Used branchless signed-code classification and a single unsigned range check for map payload bounds.
- Flattened container skipping into an iterative count of pending values, avoiding recursive stack growth and auxiliary allocations.
- Parsed string-convertible values from temporary UTF-16 storage: stack space for small UTF-8 inputs and pooled arrays for larger inputs.
- Added concise English XML documentation to previously undocumented public members and corrected inaccurate existing descriptions.

An additional experiment split string and array-header reads into separate fast and slow paths. It did not demonstrate a reliable improvement over candidate 2 and was reverted. Candidate 3 measured 42.49 ns for binary deserialization versus 40.75 ns for candidate 2, with substantial variation. The final implementation retains candidate 2's header/string structure and adds the separately measured allocation-free parsing path.

## Correctness fixes

- `TryReadUInt64` rejects negative integers and leaves the reader unchanged on failure instead of wrapping negative values into large positive integers.
- Truncated identifier and 128-bit extension headers throw EndOfStreamException instead of returning empty data or reporting a misleading format error.
- Pooled binary reads expose exactly the payload length, not the pool bucket's extra capacity. Empty payloads do not rent a buffer.
- Array/map lengths that cannot be represented by the reader are rejected.
- Signed writer header arguments reject negative lengths/counts; binary/string size arithmetic and sequence-length conversions check overflow.
- Extension headers preserve the full unsigned 32-bit length without attempting an overflowing payload reservation.
- UTF-16 span writing handles empty spans and supplies the encoder with the actual available destination capacity.
- Truncated raw reads construct a single exception instead of constructing a second exception just to obtain its message.

## Validation and reproduction

`XUnitTest`: **369 passed, 0 failed, 0 skipped**. The 14 added tests cover integer widths and limits, fixints, malformed/truncated inputs, reader rollback, exact pooled lengths, 100,000 nested arrays, UTF-8 boundaries and replacement fallback, wire byte order, floating-point bit patterns, all 256 format codes, and temporary-buffer string parsing.

The final library build completed with **0 warnings and 0 errors**. `git diff --check` passed.

Run from the repository root after restoring dependencies:

```powershell
dotnet build Tinyhand/Tinyhand.csproj -c Release --no-restore
dotnet test --project XUnitTest/XUnitTest.csproj -c Release --no-restore
dotnet run --project Benchmark/Benchmark.csproj -c Release --no-restore -- --filter '*Utf8Benchmark*' --artifacts BenchmarkDotNet.Artifacts/io-final
```

Local raw reports are under `BenchmarkDotNet.Artifacts/io-baseline`, `io-candidate1`, `io-candidate2`, `io-candidate3`, and `io-final`. JIT output is in `BenchmarkDotNet.Artifacts/io-jit.txt`. These generated artifacts are ignored by Git; this report retains the results.
