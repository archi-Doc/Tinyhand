using System;
using System.Diagnostics.CodeAnalysis;
using Arc.Collections.HotMethod;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Playground;

[TinyhandObject]
public partial class PartialImplementationClass
{
    public PartialImplementationClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = "Test";

    [Key(1)]
    public int Id { get; set; }

    static void ITinyhandSerializable<PartialImplementationClass>.Serialize(ref TinyhandWriter writer, scoped ref PartialImplementationClass? v, TinyhandSerializerOptions options)
    {
        if (v == null)
        {
            writer.WriteNil();
            return;
        }

        if (!options.IsSignatureMode) writer.WriteArrayHeader(2);
        writer.Write(v.Name);
        writer.Write(v.Id);
    }
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var types = Tinyhand.Resolvers.StandardResolver.Instance.GetInstantiableTypes();
    }
}
