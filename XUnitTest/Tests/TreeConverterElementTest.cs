// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Tinyhand;
using Tinyhand.Tree;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class TreeElementClass
{
    public int Number { get; set; } = 12345;

    public string Text { get; set; } = "text";

    public double Double { get; set; } = 1.5;

    public bool Bool { get; set; } = true;

    public string? Null { get; set; }

    public byte[] Binary { get; set; } = [1, 2, 3];

    public int[] Array { get; set; } = [10, 20, 30];

    public Dictionary<string, int> Map { get; set; } = new() { { "a", 1 }, { "b", 2 } };
}

/// <summary>
/// Covers the Element (tree) conversions of <see cref="TinyhandTreeConverter"/>: binary to tree,
/// tree to binary, and the position lookup used to report where a deserialization error occurred.
/// </summary>
public class TreeConverterElementTest
{
    private static readonly TinyhandSerializerOptions Options = TinyhandSerializerOptions.Standard;

    [Fact]
    public void BinaryToElementAndBack()
    {
        var c = new TreeElementClass();
        var binary = TinyhandSerializer.Serialize(c);

        TinyhandTreeConverter.FromBinaryToElement(binary, out var element, Options);
        element.Type.Is(ElementType.Group);

        // The tree converts back to the identical binary.
        TinyhandTreeConverter.FromElementToBinary(element, out var binary2, Options);
        binary2.SequenceEqual(binary).IsTrue();

        // And the tree deserializes to an equal object.
        var c2 = TinyhandSerializer.DeserializeFromElement<TreeElementClass>(element, Options);
        c.IsStructuralEqual(c2);
    }

    [Fact]
    public void ElementRoundTripForEveryValueType()
    {
        // Each primitive maps to a distinct Value element type.
        var binary = TinyhandSerializer.Serialize<object?[]>(
        [
            1,
            -1,
            long.MaxValue,
            1.5d,
            true,
            false,
            null,
            "text",
            new byte[] { 1, 2, 3 },
            new int[] { 1, 2 },
        ]);

        TinyhandTreeConverter.FromBinaryToElement(binary, out var element, Options);
        var group = (Group)element;
        group.ElementList.Count.Is(10);

        ((Value)group.ElementList[0]).ValueType.Is(ValueElementType.Value_Long);
        ((Value)group.ElementList[1]).ValueType.Is(ValueElementType.Value_Long);
        ((Value)group.ElementList[2]).ValueType.Is(ValueElementType.Value_Long);
        ((Value)group.ElementList[3]).ValueType.Is(ValueElementType.Value_Double);
        ((Value)group.ElementList[4]).ValueType.Is(ValueElementType.Value_Bool);
        ((Value)group.ElementList[6]).ValueType.Is(ValueElementType.Value_Null);
        ((Value)group.ElementList[7]).ValueType.Is(ValueElementType.Value_String);
        ((Value)group.ElementList[8]).ValueType.Is(ValueElementType.Value_Binary);
        group.ElementList[9].Type.Is(ElementType.Group);

        // The tree keeps the values, but the integers are rewritten in the most compact encoding
        // (an object[] stores each integer in a fixed width to preserve its CLR type), so the
        // binary is compared through the values rather than byte by byte.
        TinyhandTreeConverter.FromElementToBinary(element, out var binary2, Options);
        var values = TinyhandSerializer.Deserialize<object?[]>(binary2)!;
        values.Length.Is(10);
        Convert.ToInt64(values[0]).Is(1L);
        Convert.ToInt64(values[1]).Is(-1L);
        Convert.ToInt64(values[2]).Is(long.MaxValue);
        Convert.ToDouble(values[3]).Is(1.5d);
        values[4].Is((object)true);
        values[5].Is((object)false);
        values[6].IsNull();
        values[7].Is((object)"text");
        ((byte[])values[8]!).SequenceEqual(new byte[] { 1, 2, 3 }).IsTrue();
    }

    [Fact]
    public void EmptyAndNestedGroups()
    {
        foreach (var text in new[] { "{}", "{{}}", "{a = 1}", "{a = {b = {c = 1}}}", "{1, 2, 3}" })
        {
            var element = TinyhandParser.Parse(text);
            TinyhandTreeConverter.FromElementToBinary(element, out var binary, Options);

            // The binary converts back to a tree that composes to the same binary.
            TinyhandTreeConverter.FromBinaryToElement(binary, out var element2, Options);
            TinyhandTreeConverter.FromElementToBinary(element2, out var binary2, Options);
            binary2.SequenceEqual(binary).IsTrue();
        }
    }

    [Fact]
    public void GetElementFromPosition()
    {
        var c = new TreeElementClass();
        var binary = TinyhandSerializer.Serialize(c);
        TinyhandTreeConverter.FromBinaryToElement(binary, out var element, Options);

        // Position 0 is before any element has been written, so nothing precedes it.
        TinyhandTreeConverter.GetElementFromPosition(element, 0, Options).IsNull();

        // Every later position resolves to an element of the tree.
        for (var position = 1; position < binary.Length; position++)
        {
            TinyhandTreeConverter.GetElementFromPosition(element, position, Options).IsNotNull();
        }

        // A position past the end has no element after it, so the search yields nothing.
        TinyhandTreeConverter.GetElementFromPosition(element, binary.Length + 1000, Options).IsNull();
    }

    [Fact]
    public void DeserializeFromElementReportsInvalidType()
    {
        // A tree whose value has the wrong type must fail with a Tinyhand error that names the position.
        var element = TinyhandParser.Parse("Number = \"not a number\"");
        var ex = Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeFromElement<TreeElementClass>(element, Options));
        ex.InnerException.IsNotNull();
    }

    [Fact]
    public void ComposeMatchesSerializeToString()
    {
        var c = new TreeElementClass();
        var binary = TinyhandSerializer.Serialize(c);
        TinyhandTreeConverter.FromBinaryToElement(binary, out var element, Options);

        // UseContextualInformation reproduces the line feeds and comments captured while parsing,
        // so it is only meaningful for a parsed tree (see ComposeKeepsContextualInformation).
        foreach (var option in new[] { TinyhandComposeOption.Standard, TinyhandComposeOption.Simple, TinyhandComposeOption.Strict })
        {
            var composed = TinyhandComposer.ComposeToString(element, option);
            composed.IsNotNull();

            // The composed text parses back into an equivalent tree.
            var element2 = TinyhandParser.Parse(composed);
            TinyhandTreeConverter.FromElementToBinary(element2, out var binary2, Options);
            Assert.True(Convert.ToHexString(binary) == Convert.ToHexString(binary2), option + " | " + composed);
        }
    }

    [Fact]
    public void ComposeKeepsContextualInformation()
    {
        // A parsed tree carries the comments and line feeds of the source, and
        // UseContextualInformation puts them back. Nesting is reproduced from those line feeds,
        // so this mode expects the indented layout rather than braces.
        const string Text = "// header\na = 1\nb =\n  c = 2\n  d = 3\n";
        var element = TinyhandParser.Parse(Text, TinyhandParserOptions.ContextualInformation);

        var composed = TinyhandComposer.ComposeToString(element, TinyhandComposeOption.UseContextualInformation);
        composed.Contains("// header").IsTrue();

        // And the result still describes the same data.
        TinyhandTreeConverter.FromElementToBinary(TinyhandParser.Parse(composed), out var binary, Options);
        TinyhandTreeConverter.FromElementToBinary(TinyhandParser.Parse(Text), out var expected, Options);
        Assert.True(Convert.ToHexString(expected) == Convert.ToHexString(binary), "composed: <" + composed + ">");
    }

    [Fact]
    public void ComposeNestedGroupsKeepStructure()
    {
        // Simple and Strict compose a single line, so nested groups must be braced;
        // Standard expresses the same nesting with indentation.
        var element = TinyhandParser.Parse("a = { b = { c = 1 }, d = 2 }, e = { 1, 2 }");
        TinyhandTreeConverter.FromElementToBinary(element, out var expected, Options);

        foreach (var option in new[] { TinyhandComposeOption.Standard, TinyhandComposeOption.Simple, TinyhandComposeOption.Strict })
        {
            var composed = TinyhandComposer.ComposeToString(element, option);
            TinyhandTreeConverter.FromElementToBinary(TinyhandParser.Parse(composed), out var actual, Options);
            Assert.True(Convert.ToHexString(expected) == Convert.ToHexString(actual), option + " | " + composed);
        }
    }

    [Fact]
    public void ComposeToBufferWriter()
    {
        var element = TinyhandParser.Parse("a = 1, b = \"text\"");

        var array = TinyhandComposer.Compose(element);
        var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
        TinyhandComposer.Compose(bufferWriter, element);

        Assert.Equal(Convert.ToHexString(array), Convert.ToHexString(bufferWriter.WrittenSpan));
    }
}
