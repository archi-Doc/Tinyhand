// Bounded local probes for the September 2026 security review.
// These characterize observed behavior, rather than assert that it is desirable.
using System.Buffers.Binary;
using System.Diagnostics;
using Arc.Collections;
using Tinyhand;
using Tinyhand.IO;

var options = TinyhandSerializerOptions.Standard with
{
    Security = TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(64),
};
Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Default: resistant={TinyhandSerializer.DefaultOptions.Security.HashCollisionResistant}, depth={TinyhandSerializer.DefaultOptions.Security.MaximumObjectGraphDepth}");

// The input construction is from the .NET team's System.HashCode security design doc.
// https://github.com/dotnet/runtime/blob/main/docs/design/security/System.HashCode.md
var colliding = new long[4096];
for (uint i = 0; i < colliding.Length; i++)
{
    uint high = unchecked(i * 0xA89ED915u << 15);
    uint low = unchecked(i * 0x412BDE25u);
    colliding[i] = unchecked((long)(((ulong)high << 32) | low));
}
var comparer = options.Security.GetEqualityComparer<long>();
var groups = colliding.GroupBy(comparer.GetHashCode).ToArray();
Console.WriteLine($"Untrusted Int64: distinctKeys={colliding.Distinct().Count()}, distinctHashes={groups.Length}, largestCollisionGroup={groups.Max(x => x.Count())}");
var doubles = colliding.Select(BitConverter.Int64BitsToDouble).Where(x => !double.IsNaN(x)).ToArray();
var doubleGroups = doubles.GroupBy(options.Security.GetEqualityComparer<double>().GetHashCode).ToArray();
Console.WriteLine($"Untrusted Double: distinctKeys={doubles.Distinct().Count()}, distinctHashes={doubleGroups.Length}, largestCollisionGroup={doubleGroups.Max(x => x.Count())}");

foreach (var count in new[] { 1024, 2048, 4096 })
{
    var source = colliding.Take(count).ToDictionary(x => x, _ => 0);
    var bytes = TinyhandSerializer.Serialize(source);
    TinyhandSerializer.Deserialize<Dictionary<long, int>>(bytes, options); // Warm-up.
    var watch = Stopwatch.StartNew();
    for (var repeat = 0; repeat < 3; repeat++)
    {
        TinyhandSerializer.Deserialize<Dictionary<long, int>>(bytes, options);
    }
    Console.WriteLine($"Untrusted Dictionary<Int64,int>: count={count}, bytes={bytes.Length}, averageMs={watch.Elapsed.TotalMilliseconds / 3:F3}");
}

// Default Int64 hashing is simpler still: equal high and low halves XOR to zero.
var defaultCollisions = Enumerable.Range(0, 1024).Select(i => 0x100000001L * i).ToArray();
var map = new UnorderedMap<long, int>();
foreach (var key in defaultCollisions) map.Add(key, 0);
var arcMap = TinyhandSerializer.Deserialize<UnorderedMap<long, int>>(TinyhandSerializer.Serialize(map), options)!;
Console.WriteLine($"Arc UnorderedMap: count={arcMap.Count}, defaultComparer={ReferenceEquals(arcMap.Comparer, EqualityComparer<long>.Default)}, distinctHashes={defaultCollisions.Select(arcMap.Comparer.GetHashCode).Distinct().Count()}");
var arcObject = new UnorderedMap<object, int>();
arcObject.Add(1, 1);
Console.WriteLine($"Arc object keys accepted={TinyhandSerializer.TryDeserialize<UnorderedMap<object, int>>(TinyhandSerializer.Serialize(arcObject), out _, options)}");
var set = new UnorderedSet<long>();
foreach (var key in defaultCollisions) set.Add(key);
var arcSet = TinyhandSerializer.Deserialize<UnorderedSet<long>>(TinyhandSerializer.Serialize(set), options)!;
Console.WriteLine($"Arc UnorderedSet: count={arcSet.Count}, distinctHashes={defaultCollisions.Select(arcSet.Comparer.GetHashCode).Distinct().Count()}");

// A fresh generated DTO also has initialized collection members reused.
var dto = new DictionaryPayload();
foreach (var key in defaultCollisions) dto.Values.Add(key, 0);
var decodedDto = TinyhandSerializer.Deserialize<DictionaryPayload>(TinyhandSerializer.Serialize(dto), options)!;
Console.WriteLine($"Generated DTO dictionary: count={decodedDto.Values.Count}, defaultComparer={ReferenceEquals(decodedDto.Values.Comparer, EqualityComparer<long>.Default)}, distinctHashes={defaultCollisions.Select(decodedDto.Values.Comparer.GetHashCode).Distinct().Count()}");
var noReuse = new NoReuseDictionaryPayload { Values = dto.Values };
var decodedNoReuse = TinyhandSerializer.Deserialize<NoReuseDictionaryPayload>(TinyhandSerializer.Serialize(noReuse), options)!;
Console.WriteLine($"ReuseMembers=false DTO dictionary: defaultComparer={ReferenceEquals(decodedNoReuse.Values.Comparer, EqualityComparer<long>.Default)}");

// Depth is bounded, but allocations made before a truncated input fails still accumulate.
var nested = new byte[64 * 5 + 8192];
for (var i = 0; i < 64; i++)
{
    nested[i * 5] = 0xdd;
    BinaryPrimitives.WriteInt32BigEndian(nested.AsSpan(i * 5 + 1, 4), 8192);
}
nested.AsSpan(64 * 5).Fill(0xc0);
TinyhandSerializer.TryDeserialize<object>(nested, out _, options);
var before = GC.GetAllocatedBytesForCurrentThread();
var accepted = TinyhandSerializer.TryDeserialize<object>(nested, out _, options);
Console.WriteLine($"Nested allocation: inputBytes={nested.Length}, accepted={accepted}, allocatedBytes={GC.GetAllocatedBytesForCurrentThread() - before}");

var depthBytes = Enumerable.Repeat((byte)0x91, 100).Append((byte)0xc0).ToArray();
Console.WriteLine($"100 nested arrays: trustedAccepted={TinyhandSerializer.TryDeserialize<object>(depthBytes, out _, TinyhandSerializerOptions.Standard)}, depth64Accepted={TinyhandSerializer.TryDeserialize<object>(depthBytes, out _, options)}");
var skipBytes = Enumerable.Repeat((byte)0x91, 100_000).Append((byte)0xc0).ToArray();
var skipReader = new TinyhandReader(skipBytes);
skipReader.Skip();
Console.WriteLine($"Iterative Skip: depth=100000, consumed={skipReader.Consumed}, end={skipReader.End}");

var lz4 = TinyhandSerializerOptions.Lz4 with { Security = options.Security };
var compressed = TinyhandSerializer.Serialize(new byte[1_048_576], lz4);
var decompressed = TinyhandSerializer.Deserialize<byte[]>(compressed, lz4)!;
Console.WriteLine($"LZ4 expansion: compressedBytes={compressed.Length}, resultBytes={decompressed.Length}");
var first = TinyhandSerializer.Deserialize<int>(new byte[] { 42, 99 }, out var read, options);
Console.WriteLine($"Trailing bytes: result={first}, read={read}, supplied=2");
byte[] innerTrailing = [0x92, 0xc7, 1, 98, 2, 0xc4, 3, 0x20, 42, 99];
var innerFirst = TinyhandSerializer.Deserialize<int>(innerTrailing, out var outerRead, lz4);
Console.WriteLine($"LZ4 inner trailing bytes: result={innerFirst}, outerRead={outerRead}, supplied={innerTrailing.Length}");

using (var writer = TinyhandWriter.CreateFromBytePool())
{
    writer.WriteArrayHeader(1);
    writer.Write(new string('x', 10_000));
    var bytes = writer.FlushAndGetArray();
    TinyhandSerializer.Deserialize<LimitedPayload>(bytes, options);
    var allocationStart = GC.GetAllocatedBytesForCurrentThread();
    var result = TinyhandSerializer.Deserialize<LimitedPayload>(bytes, options)!;
    Console.WriteLine($"MaxLength(3): decodedLength={result.Name.Length}, allocatedBytes={GC.GetAllocatedBytesForCurrentThread() - allocationStart}");
}

byte[] invalidDecimal = new byte[18];
invalidDecimal[0] = 0xc4;
invalidDecimal[1] = 16;
BinaryPrimitives.WriteInt32LittleEndian(invalidDecimal.AsSpan(2, 4), 0x00ff0000);
var dec = TinyhandSerializer.Deserialize<decimal>(invalidDecimal, options);
var decBits = decimal.GetBits(dec);
var acceptedByConstructor = true;
try { _ = new decimal(decBits); } catch (ArgumentException) { acceptedByConstructor = false; }
Console.WriteLine($"NativeDecimal: flags=0x{decBits[3]:X8}, acceptedByDecimalConstructor={acceptedByConstructor}");

var invalidUtf8 = TinyhandSerializer.Deserialize<string>(new byte[] { 0xa1, 0xff }, options)!;
Console.WriteLine($"Invalid UTF8: accepted=true, firstCodePoint=U+{(int)invalidUtf8[0]:X4}");
byte[] duplicate = [0x82, 0xa1, (byte)'x', 1, 0xa1, (byte)'x', 2];
var dictDuplicate = TinyhandSerializer.Deserialize<Dictionary<string, int>>(duplicate, options)!;
var dtoDuplicate = TinyhandSerializer.Deserialize<StringPayload>(duplicate, options)!;
Console.WriteLine($"Duplicate keys: dictionaryValue={dictDuplicate["x"]}, generatedDtoValue={dtoDuplicate.X}");

var partial = new PairPayload { First = 1, Second = 2 };
var retained = partial;
var partialReader = new TinyhandReader(new byte[] { 0x92, 9, 0xc1 });
var partialSuccess = TinyhandSerializer.TryDeserializeObject(ref partialReader, ref partial, options);
Console.WriteLine($"Failed ref deserialization: success={partialSuccess}, refCleared={partial is null}, retainedFirst={retained.First}, retainedSecond={retained.Second}");

[TinyhandObject]
public partial class DictionaryPayload
{
    [Key(0)]
    public Dictionary<long, int> Values { get; set; } = new();
}

[TinyhandObject(ReuseMembers = false)]
public partial class NoReuseDictionaryPayload
{
    [Key(0)]
    public Dictionary<long, int> Values { get; set; } = new();
}

[TinyhandObject]
public partial class LimitedPayload
{
    [Key(0, PropertyName = "Name"), MaxLength(3)]
    private string _name = string.Empty;
}

[TinyhandObject]
public partial class StringPayload
{
    [Key("x")]
    public int X { get; set; }
}

[TinyhandObject]
public partial class PairPayload
{
    [Key(0)]
    public int First { get; set; }

    [Key(1)]
    public int Second { get; set; }
}
