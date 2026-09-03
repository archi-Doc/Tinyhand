// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using System.Text;
using Tinyhand;
using Tinyhand.Tree;
using Xunit;

namespace XUnitTest.Tests;

/// <summary>
/// Covers <see cref="TinyhandTreeHelper"/>, the tree query API used to read Tinyhand documents
/// (identifier = value, identifier = { ... }) without deserializing them into an object.
/// </summary>
public class TreeHelperTest
{
    private static Group Parse(string text) => (Group)TinyhandParser.Parse(text);

    [Fact]
    public void IsAssigned()
    {
        var group = Parse("a = 1, 2");
        var assignment = group.ElementList[0];
        assignment.Type.Is(ElementType.Assignment);

        ((Assignment)assignment).LeftElement!.IsAssigned().IsTrue();
        ((Assignment)assignment).RightElement!.IsAssigned().IsTrue();

        // A bare value at the top level has the group as its parent.
        group.ElementList[1].IsAssigned().IsFalse();
    }

    [Fact]
    public void IsTrueAndIsFalse()
    {
        var group = Parse("t = true, f = false, one = 1, zero = 0, minus = -1, d = 1.5, dzero = 0.0, s = \"True\", sf = \"FALSE\", other = \"x\"");

        Value Right(string identifier)
        {
            foreach (var x in group)
            {
                if (x.TryGetRight_Value(identifier, out var v))
                {
                    return v;
                }
            }

            throw new System.InvalidOperationException(identifier);
        }

        Right("t").IsTrue().IsTrue();
        Right("t").IsFalse().IsFalse();
        Right("f").IsFalse().IsTrue();
        Right("f").IsTrue().IsFalse();

        Right("one").IsTrue().IsTrue();
        Right("zero").IsFalse().IsTrue();

        // Only a positive number is true and only zero is false, so a negative number is neither.
        Right("minus").IsTrue().IsFalse();
        Right("minus").IsFalse().IsFalse();

        Right("d").IsTrue().IsTrue();
        Right("dzero").IsFalse().IsTrue();

        // Strings are compared case-insensitively.
        Right("s").IsTrue().IsTrue();
        Right("sf").IsFalse().IsTrue();
        Right("other").IsTrue().IsFalse();
        Right("other").IsFalse().IsFalse();
    }

    [Fact]
    public void TryGetLeftIdentifier()
    {
        var group = Parse("name = \"value\"");
        var element = group.ElementList[0];

        element.TryGetLeft_IdentifierUtf16(out var utf16).IsTrue();
        utf16.Is("name");

        element.TryGetLeft_IdentifierUtf8(out var utf8).IsTrue();
        Encoding.UTF8.GetString(utf8!).Is("name");

        // A value that is not an assignment has no left identifier.
        Parse("1").ElementList[0].TryGetLeft_IdentifierUtf16(out _).IsFalse();

        // A quoted left side is a string, not an identifier.
        Parse("\"name\" = 1").ElementList[0].TryGetLeft_IdentifierUtf16(out _).IsFalse();
    }

    [Fact]
    public void TryGetRight()
    {
        var group = Parse("s = \"text\", n = 123, g = { inner = 1 }");
        var s = group.ElementList[0];
        var n = group.ElementList[1];
        var g = group.ElementList[2];

        s.TryGetRight_Value(out var value).IsTrue();
        value.ValueType.Is(ValueElementType.Value_String);

        s.TryGetRight_Value_String(out var valueString).IsTrue();
        valueString.Utf16.Is("text");
        s.TryGetRight_Value_Long(out _).IsFalse();

        n.TryGetRight_Value_Long(out var valueLong).IsTrue();
        valueLong.ValueLong.Is(123L);
        n.TryGetRight_Value_String(out _).IsFalse();

        g.TryGetRight_Group(out var innerGroup).IsTrue();
        innerGroup.ElementList.Count.Is(1);
        g.TryGetRight_Value(out _).IsFalse();

        // A non-assignment element has no right side.
        Parse("1").ElementList[0].TryGetRight_Value(out _).IsFalse();
    }

    [Fact]
    public void TryGetRightByIdentifier()
    {
        var group = Parse("s = \"text\", n = 123");
        var s = group.ElementList[0];
        var n = group.ElementList[1];

        s.TryGetRight_Value("s", out var value).IsTrue();
        ((Value_String)value).Utf16.Is("text");
        s.TryGetRight_Value("other", out _).IsFalse();

        s.TryGetRight_Value_String("s", out var valueString).IsTrue();
        valueString.Utf16.Is("text");
        s.TryGetRight_Value_Long("s", out _).IsFalse();

        n.TryGetRight_Value_Long("n", out var valueLong).IsTrue();
        valueLong.ValueLong.Is(123L);
        n.TryGetRight_Value_String("n", out _).IsFalse();
    }

    [Fact]
    public void TryGetRightGroup()
    {
        // left = { identifier = value }
        var nested = Parse("outer = { inner = \"text\", number = 1 }").ElementList[0];
        nested.TryGetRightGroup_Value("inner", out var value).IsTrue();
        ((Value_String)value).Utf16.Is("text");
        nested.TryGetRightGroup_Value_String("inner", out var valueString).IsTrue();
        valueString.Utf16.Is("text");
        nested.TryGetRightGroup_Value("missing", out _).IsFalse();

        // A null identifier accepts a bare value or the first value inside the group.
        var bare = Parse("outer = \"text\"").ElementList[0];
        bare.TryGetRightGroup_Value(null, out var bareValue).IsTrue();
        ((Value_String)bareValue).Utf16.Is("text");
        bare.TryGetRightGroup_Value_String(null, out var bareString).IsTrue();
        bareString.Utf16.Is("text");

        var wrapped = Parse("outer = { \"text\" }").ElementList[0];
        wrapped.TryGetRightGroup_Value(null, out var wrappedValue).IsTrue();
        ((Value_String)wrappedValue).Utf16.Is("text");

        // The identifier is required when it is not null, even for a bare value.
        bare.TryGetRightGroup_Value("inner", out _).IsFalse();

        // A non-assignment element has no right group.
        Parse("1").ElementList[0].TryGetRightGroup_Value(null, out _).IsFalse();
    }

    [Fact]
    public void DeepCopyIsIndependent()
    {
        var group = Parse("a = 1, b = { c = \"text\" }, /* comment */ d = 2");
        var copy = (Group)group.DeepCopy();

        copy.ElementList.Count.Is(group.ElementList.Count);

        // The copy has its own elements: removing from it leaves the original intact.
        var removed = copy.ElementList[0];
        copy.RemoveChild(removed);
        copy.ElementList.Count.Is(group.ElementList.Count - 1);
        removed.Parent.IsNull();

        // The copied children point at the copy, not at the original.
        foreach (var x in copy)
        {
            (x.Parent == copy).IsTrue();
        }

        // The original still composes to the same text as before the copy was modified.
        var a = group.ElementList[0];
        a.TryGetRight_Value_Long("a", out var valueLong).IsTrue();
        valueLong.ValueLong.Is(1L);
    }

    [Fact]
    public void RemoveChild()
    {
        var group = Parse("a = 1, b = 2");
        var first = group.ElementList[0];
        (first.Parent == group).IsTrue();

        group.RemoveChild(first);
        group.ElementList.Count.Is(1);
        first.Parent.IsNull();

        // Removing an element that is not a child is a no-op.
        group.RemoveChild(first);
        group.ElementList.Count.Is(1);
    }
}
