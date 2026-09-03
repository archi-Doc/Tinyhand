// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Linq;
using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class BufferWriterClass
{
    public int Number { get; set; } = 1;

    public string Text { get; set; } = "text";

    public int[] Array { get; set; } = [1, 2, 3];
}

/// <summary>
/// The <see cref="IBufferWriter{T}"/> overloads buffer into a span and only hand the bytes to the
/// caller's writer when they are flushed, so each one is checked against the array-returning overload.
/// </summary>
public class BufferWriterApiTest
{
    [Fact]
    public void Serialize()
    {
        var c = new BufferWriterClass();

        foreach (var options in new[] { TinyhandSerializerOptions.Standard, TinyhandSerializerOptions.Lz4 })
        {
            var expected = TinyhandSerializer.Serialize(c, options);

            var bufferWriter = new ArrayBufferWriter<byte>();
            TinyhandSerializer.Serialize(bufferWriter, c, options);

            bufferWriter.WrittenSpan.SequenceEqual(expected).IsTrue();
            TinyhandSerializer.Deserialize<BufferWriterClass>(bufferWriter.WrittenSpan, options).IsStructuralEqual(c);
        }
    }

    [Fact]
    public void SerializeToUtf8()
    {
        var c = new BufferWriterClass();
        var expected = TinyhandSerializer.SerializeToUtf8(c);

        var bufferWriter = new ArrayBufferWriter<byte>();
        TinyhandSerializer.SerializeToUtf8(bufferWriter, c);

        Assert.Equal(Encoding.UTF8.GetString(expected), Encoding.UTF8.GetString(bufferWriter.WrittenSpan));

        // The text written to the buffer writer must be readable by the matching deserializer.
        TinyhandSerializer.DeserializeFromUtf8<BufferWriterClass>(bufferWriter.WrittenSpan).IsStructuralEqual(c);
    }

    [Fact]
    public void Compose()
    {
        var element = TinyhandParser.Parse("a = 1, b = \"text\", c = { d = 2 }");

        foreach (var option in new[] { TinyhandComposeOption.Standard, TinyhandComposeOption.Simple, TinyhandComposeOption.Strict })
        {
            var expected = TinyhandComposer.Compose(element, option);

            var bufferWriter = new ArrayBufferWriter<byte>();
            TinyhandComposer.Compose(bufferWriter, element, option);

            Assert.Equal(Convert.ToHexString(expected), Convert.ToHexString(bufferWriter.WrittenSpan));
        }
    }

    [Fact]
    public void LargeValueSpansMultipleBuffers()
    {
        // A value larger than the initial buffer forces the writer to acquire more than one span,
        // so this checks that every span reaches the buffer writer.
        var c = new BufferWriterClass { Array = Enumerable.Range(0, 200_000).ToArray(), };
        var expected = TinyhandSerializer.Serialize(c);

        var bufferWriter = new ArrayBufferWriter<byte>();
        TinyhandSerializer.Serialize(bufferWriter, c);

        bufferWriter.WrittenSpan.SequenceEqual(expected).IsTrue();
        TinyhandSerializer.Deserialize<BufferWriterClass>(bufferWriter.WrittenSpan)!.Array.Length.Is(200_000);
    }
}
