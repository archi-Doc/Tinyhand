# Runtime and coverage audit

Reviewed on September 5–6, 2026 with .NET SDK 10.0.400 and .NET 10.0.11 on Windows x64.

## Corrections

| Area | Finding and correction |
| --- | --- |
| Nested serialization | Active binary, text, and LZ4 writers could share a thread-local buffer and overwrite each other. Writers now reserve that buffer until disposal; nested calls rent separate storage. |
| Default options | The one-argument binary `Deserialize<T>` overload bypassed LZ4 processing even when `DefaultOptions` enabled compression. It now honors the same options as the other binary overloads. |
| Text streams | The parser used one `Read` call and required `Length`/`Position`, truncating short reads and rejecting non-seekable streams. It now reads the complete input and returns temporary storage in `finally` blocks. |
| File access | File parsing requested read/write access. Both synchronous and asynchronous parsing now open files for reading with read sharing. |
| Immutable arrays | Empty input produced a default `ImmutableArray<T>`, and cloning a default instance accessed its invalid length. Empty and default instances now remain distinct. |
| Generator attributes | Static registration accessed `TypedConstant.Value` for array arguments, raising `THAOT999`. It now visits array elements and handles null arrays. The shared attribute reader also handles null/empty arrays without crashing or shifting later constructor arguments. |

Existing formatter reuse behavior is preserved. Low-level `ref` overloads may retain a value on nil or merge incoming dictionary entries; the existing tests explicitly require this behavior. The return-value APIs start from a default value and provide a fresh result.

Commented-out code was retained. The benchmark entry point no longer executes unrelated primitive benchmarks before running the requested benchmark selection.

## Allocation changes

- `List<byte>` serializes directly from its backing span and deserializes into one list allocation and its backing array.
- Borrowed writer sequences refer to the existing buffer instead of copying it.
- Primitive empty-array reconstruction and cloning return `Array.Empty<T>()`; the T4 template and checked-in generated source agree.
- Option flag checks use bit operations, avoiding `Enum.HasFlag` boxing before JIT optimization.
- Localized string loading parses the source stream directly instead of copying through a `MemoryStream` and another array.
- Temporary unescape and parser buffers are returned even when parsing fails.

### Measurements

The benchmark uses a 1,024-byte list, a preallocated output writer/stream, and warmed methods. BenchmarkDotNet 0.15.8 ran on an Intel Core i9-13900K with workstation GC, one launch, three warmup iterations, and three measured iterations. Values are per operation; timings are short-run observations, not guarantees for other inputs or machines. Deserialization retains the necessary list and backing-array allocation.

| Operation | Before (ns) | After (ns) | Before (B) | After (B) |
| --- | ---: | ---: | ---: | ---: |
| Serialize byte list | 58.332 | 21.796 | 1,048 | 0 |
| Deserialize byte list | 76.888 | 40.743 | 2,128 | 1,080 |
| Serialize to stream | 15.676 | 14.534 | 32 | 0 |
| Serialize with LZ4 | 177.711 | 99.367 | 2,160 | 56 |
| Borrow writer sequence | 6.022 | 2.945 | 32 | 0 |
| Reconstruct empty array | 6.375 | 3.438 | 24 | 0 |
| Clone empty array | 5.754 | 3.023 | 24 | 0 |

Reproduce with:

```sh
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter '*AllocationAuditBenchmark*' --job short --warmupCount 3 --iterationCount 3 --launchCount 1
```

Local raw reports are saved under `artifacts/audit/benchmark-before` and `artifacts/audit/benchmark-final`. They are ignored build artifacts; the benchmark source and this summary are tracked.

## Coverage

Microsoft Testing Platform measured the three implementation assemblies with `XUnitTest/coverage.config`. Test assemblies and dependencies are excluded. The baseline ran 563 tests. The final suite contains 599 tests; its coverage run excludes three exact-allocation tests because instrumentation changes allocation behavior.

| Assembly | Line coverage before | Line coverage after | Branch coverage before | Branch coverage after |
| --- | ---: | ---: | ---: | ---: |
| Tinyhand | 79.40% | 79.90% | 76.83% | 77.69% |
| TinyhandGenerator | 41.84% | 50.09% | 35.04% | 39.48% |
| TinyhandProcessor | 59.77% | 59.77% | 55.78% | 55.78% |
| Total | 61.00% | 65.21% | 51.13% | 54.22% |

Covered lines increased from 19,441 of 31,872 to 20,847 of 31,971. Representative generator files improved from 20.4% to 61.9% for primitive coders, 7.1% to 61.4% for enum coders, and 9.0% to 73.0% for callback handling. Runtime text-parser coverage is 92.6%.

New tests cover short reads, non-seekable and sliced streams, read-only file access, nested binary/text/LZ4 serialization, exception cleanup, buffer allocations, immutable-array boundaries, default compression, attribute arrays, and generated scalar/nullable/array/list/enum/callback/locking code. Generator tests inspect errors in the final compilation, not only generated strings.

Coverage limits:

- Generator coverage records calls made inside tests. Generation performed by the build is outside the measurement, even when its output is exercised by runtime tests.
- Processor GUI startup measurement is 1.6% covered and its command-line entry point is not covered by this unit-test run. Separate native smoke tests cover processor logging and plugin hosting.
- x86-specific compression paths, some reflection helpers, diagnostic combinations, and shared generator utilities remain partially or wholly uncovered.
- NativeAOT smoke-test execution is validated separately and is not included in these percentages.

Local Cobertura reports are saved as `artifacts/audit/coverage-before.xml` and `artifacts/audit/coverage-after.xml`.

## Validation

- The complete solution builds in Release with no warnings or errors.
- All 599 unit tests pass in Debug and Release, including the three allocation tests without coverage instrumentation.
- All 596 selected tests pass under coverage instrumentation.
- NativeAOT serializer checks include nested callbacks, byte lists, and empty/default immutable arrays.
- ValueLink 0.118.3 external-registration checks and Processor native smoke tests pass on Windows x64.
- Processor publication still reports five existing IL2091/IL2067 warnings from Arc.Unit's DI registration annotations; the runtime changes do not address that dependency.

XML comments and the README now describe default options, nullable `TryDeserialize` results, formatter reuse, buffer ownership, nested serialization, external registration, and reproducible coverage/benchmark commands. Public API signatures and the binary wire format are unchanged.
