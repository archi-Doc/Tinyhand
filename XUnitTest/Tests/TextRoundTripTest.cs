// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class TextRoundTripClass
{
    public string Text { get; set; } = string.Empty;

    public byte[] Binary { get; set; } = [];

    public double Double { get; set; }

    public long Long { get; set; }

    public ulong ULong { get; set; }

    public bool Bool { get; set; }

    public string? Null { get; set; }

    public Dictionary<string, int> Map { get; set; } = [];

    public int[] Array { get; set; } = [];
}

public class TextRoundTripTest
{
    private static void RoundTrip(TextRoundTripClass c)
    {
        var st = TinyhandSerializer.SerializeToString(c);
        var c2 = TinyhandSerializer.DeserializeFromString<TextRoundTripClass>(st);
        c.IsStructuralEqual(c2);

        // Utf8 path.
        var utf8 = TinyhandSerializer.SerializeToUtf8(c);
        var c3 = TinyhandSerializer.DeserializeFromUtf8<TextRoundTripClass>(utf8);
        c.IsStructuralEqual(c3);
    }

    [Fact]
    public void StringsRequiringEscapes()
    {
        foreach (var text in new[]
        {
            string.Empty,
            "simple",
            "with space",
            "quote\"inside",
            "back\\slash",
            "tab\there",
            "line\nfeed",
            "carriage\rreturn",
            "form\ffeed",
            "back\bspace",
            "{braces}",
            "[brackets]",
            "comma,separated",
            "equals=sign",
            "hash#comment",
            "slash/slash",
            "日本語テキスト",
            "emoji \U0001F600",
            "null",
            "true",
            "false",
            "123",
        })
        {
            RoundTrip(new TextRoundTripClass { Text = text, });
        }
    }

    [Fact]
    public void Numbers()
    {
        foreach (var value in new[] { 0d, 1d, -1d, 0.5d, -0.5d, double.MaxValue, double.MinValue, double.Epsilon, double.NaN, double.PositiveInfinity, double.NegativeInfinity, })
        {
            RoundTrip(new TextRoundTripClass { Double = value, });
        }

        foreach (var value in new[] { 0L, 1L, -1L, long.MaxValue, long.MinValue, })
        {
            RoundTrip(new TextRoundTripClass { Long = value, });
        }

        foreach (var value in new[] { 0UL, 1UL, ulong.MaxValue, })
        {
            RoundTrip(new TextRoundTripClass { ULong = value, });
        }
    }

    [Fact]
    public void BinaryAndCollections()
    {
        foreach (var length in new[] { 0, 1, 2, 3, 100, 5000 })
        {
            var binary = new byte[length];
            new Random(length).NextBytes(binary);
            RoundTrip(new TextRoundTripClass { Binary = binary, });
        }

        RoundTrip(new TextRoundTripClass
        {
            Map = new() { { "a", 1 }, { "b b", 2 }, { "\"c\"", 3 }, },
            Array = [1, 2, 3, -4],
            Bool = true,
        });
    }

    [Fact]
    public void UnicodeEscapeInSource()
    {
        // \uXXXX escapes must consume exactly four hex digits.
        var c = TinyhandSerializer.DeserializeFromString<TextRoundTripClass>("Text = \"\\u0041BCD\"");
        c!.Text.Is("ABCD");

        var c2 = TinyhandSerializer.DeserializeFromString<TextRoundTripClass>("Text = \"\\uD83D\\uDE00\"");
        c2!.Text.Is("\U0001F600");
    }
}
