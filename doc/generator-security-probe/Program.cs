using Tinyhand;
using Tinyhand.IO;

var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(4) };

var longString = new string('a', 1024 * 1024);
var writer = TinyhandWriter.CreateFromBytePool();
writer.WriteArrayHeader(1);
writer.Write(longString);
var textBytes = writer.FlushAndGetArray();
writer.Dispose();
_ = TinyhandSerializer.Deserialize<BoundedText>(new byte[] { 0x91, 0xa0 }, options);
var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
var text = TinyhandSerializer.Deserialize<BoundedText>(textBytes, options)!;
var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
Console.WriteLine($"maxLength: input={textBytes.Length}, output={text.Value.Length}, allocated={allocated}");
var plainText = TinyhandSerializer.Deserialize<PlainBoundedText>(textBytes, options)!;
Console.WriteLine($"plainMaxLength: output={plainText.Value.Length}");

var duplicate = TinyhandSerializer.Deserialize<MapDto>(new byte[] { 0x82, 0xa1, 0x41, 1, 0xa1, 0x41, 2 }, options)!;
Console.WriteLine($"duplicate: A={duplicate.A}, setters={duplicate.SetterCount}");

var deepUnknown = new byte[18];
deepUnknown[0] = 0x91;
Array.Fill(deepUnknown, (byte)0x91, 1, 16);
deepUnknown[^1] = 0xc0;
var ignored = TinyhandSerializer.Deserialize<EmptyDto>(deepUnknown, options);
Console.WriteLine("unknownDepth: accepted 16 nested arrays with MaximumObjectGraphDepth=4");

var normalDepth = new byte[18];
Array.Fill(normalDepth, (byte)0x91);
normalDepth[^1] = 0xc0;
try { _ = TinyhandSerializer.Deserialize<Node>(normalDepth, options); }
catch (Exception e) { Console.WriteLine($"knownDepth: {RootCause(e).GetType().Name}"); }

var existing = new TwoInts { A = 7, B = 8 };
var reader = new TinyhandReader(new byte[] { 0x92, 1, 0xa1, 0x78 });
try { TinyhandSerializer.DeserializeObject(ref reader, ref existing, options); }
catch (Exception e) { Console.WriteLine($"partialMutation: A={existing!.A}, B={existing.B}, error={RootCause(e).GetType().Name}"); }

Constructed.Count = 0;
try { _ = TinyhandSerializer.Deserialize<Constructed>(new byte[] { 0xc1 }, options); }
catch (Exception e) { Console.WriteLine($"constructorBeforeValidation: count={Constructed.Count}, error={RootCause(e).GetType().Name}"); }

var manyNils = new byte[132];
manyNils[0] = 0x91;
manyNils[1] = 0xdc;
manyNils[2] = 0;
manyNils[3] = 128;
Array.Fill(manyNils, (byte)0xc0, 4, 128);
Leaf.Count = 0;
var leaves = TinyhandSerializer.Deserialize<Leaves>(manyNils, options)!;
Console.WriteLine($"reconstructAmplification: input={manyNils.Length}, leafConstructors={Leaf.Count}, allocatedLeafPayload={Leaf.Count * 8192}, outputLength={leaves.Items.Length}");

static Exception RootCause(Exception e) => e.InnerException is null ? e : RootCause(e.InnerException);

[TinyhandObject]
public partial class BoundedText
{
    [Key(0, PropertyName = "Value"), MaxLength(16)]
    private string value = string.Empty;
}

[TinyhandObject]
public partial class PlainBoundedText
{
    [Key(0), MaxLength(16)] public string Value { get; set; } = string.Empty;
}

[TinyhandObject]
public partial class MapDto
{
    private int a;
    [Key("A")] public int A { get => this.a; set { this.a = value; this.SetterCount++; } }
    [IgnoreMember] public int SetterCount;
}

[TinyhandObject]
public partial class EmptyDto { }

[TinyhandObject]
public partial class Node
{
    [Key(0)] public Node? Next;
}

[TinyhandObject]
public partial class TwoInts
{
    [Key(0)] public int A;
    [Key(1)] public int B;
}

[TinyhandObject]
public partial class Constructed
{
    public static int Count;
    public Constructed() => Count++;
}

[TinyhandObject]
public partial class Leaf
{
    public static int Count;
    [IgnoreMember] public byte[] Payload = new byte[8192];
    public Leaf() => Count++;
}

[TinyhandObject]
public partial class Leaves
{
    [Key(0)] public Leaf[] Items = [];
}
