using System.Buffers;
using System.Buffers.Binary;
using System.Reflection;
using Tinyhand;
using Tinyhand.IO;

// Bounded diagnostics: compressed inputs <= 4 KiB; memory-request probe aborts before allocation.
var decompress = typeof(TinyhandSerializer)
    .GetMethod("TryDecompress", BindingFlags.Static | BindingFlags.NonPublic)!
    .CreateDelegate<Decompress>();
var options = TinyhandSerializerOptions.Lz4 with { Security = TinyhandSecurity.UntrustedData };

foreach (var outputLength in new[] { 4096 * 255, (4096 * 255) + 1 })
{
    byte[] data = new byte[14 + 4096];
    data[0] = 0x92; // array[2]
    data[1] = 0xc7; data[2] = 5; data[3] = 98; // Ext8 containing one int32
    data[4] = 0xd2;
    BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(5), outputLength);
    data[9] = 0xc6; // bin32
    BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(10), 4096);
    var reader = new TinyhandReader(data);
    var writer = new StopBeforeAllocationWriter();
    try { decompress(ref reader, writer); }
    catch (Exception ex)
    {
        Console.WriteLine($"invalid_lz4 claimed={outputLength} wire_bytes={data.Length} requested={writer.Requested} exception={ex.GetType().Name}");
    }
}

byte[] innerTrailing = [0x92, 0xc7, 1, 98, 2, 0xc4, 3, 0x20, 42, 43];
var result = TinyhandSerializer.Deserialize<int>(innerTrailing, out var read, options);
Console.WriteLine($"compressed_inner_trailing value={result} outer_bytes_consumed={read} frame_bytes={innerTrailing.Length}");

foreach (var malformed in new byte[][]
{
    [0xdd, 0xff, 0xff, 0xff, 0xff],
    [0xdf, 0xff, 0xff, 0xff, 0xff],
    [0xc6, 0xff, 0xff, 0xff, 0xff],
    [0xdb, 0xff, 0xff, 0xff, 0xff],
    [0xdd, 0x7f, 0xff, 0xff, 0xff],
})
{
    var reader = new TinyhandReader(malformed);
    try { Console.WriteLine($"skip_malformed {Convert.ToHexString(malformed)} success={reader.TrySkip()}"); }
    catch (Exception ex) { Console.WriteLine($"skip_malformed {Convert.ToHexString(malformed)} exception={ex.GetType().Name}"); }
}

byte[] nested = new byte[10_001];
Array.Fill(nested, (byte)0x91);
nested[^1] = 42;
var nestedReader = new TinyhandReader(nested);
Console.WriteLine($"skip_deep_10000 success={nestedReader.TrySkip()} consumed={nestedReader.Consumed}");

byte[] streamBytes = new byte[65_536];
streamBytes[0] = 42;
using var stream = new CountedReadStream(streamBytes);
var streamResult = TinyhandSerializer.Deserialize<int>(stream);
Console.WriteLine($"stream_first_value={streamResult} total_read={stream.TotalRead} frame_first_value_bytes=1");

delegate bool Decompress(ref TinyhandReader reader, IBufferWriter<byte> writer);

sealed class StopBeforeAllocationWriter : IBufferWriter<byte>
{
    public int Requested { get; private set; }
    public void Advance(int count) => throw new InvalidOperationException();
    public Memory<byte> GetMemory(int sizeHint = 0) { Requested = sizeHint; throw new AllocationObservedException(); }
    public Span<byte> GetSpan(int sizeHint = 0) { Requested = sizeHint; throw new AllocationObservedException(); }
}

sealed class AllocationObservedException : Exception { }

sealed class CountedReadStream(byte[] data) : Stream
{
    private int offset;
    public int TotalRead { get; private set; }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => offset; set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int start, int count) => Read(buffer.AsSpan(start, count));
    public override int Read(Span<byte> buffer)
    {
        int count = Math.Min(buffer.Length, data.Length - offset);
        data.AsSpan(offset, count).CopyTo(buffer);
        offset += count;
        TotalRead += count;
        return count;
    }
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
