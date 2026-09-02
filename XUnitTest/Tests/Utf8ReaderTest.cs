// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

public class Utf8ReaderTest
{
    private record struct Atom(TinyhandAtomType Type, string Value, int Line);

    private static List<Atom> ReadAll(string source, bool contextual = false)
    {
        var utf8 = Encoding.UTF8.GetBytes(source);
        var reader = new TinyhandUtf8Reader(utf8, contextual);
        var list = new List<Atom>();
        while (reader.Read())
        {
            list.Add(new(reader.AtomType, reader.ValueSpanToString, reader.AtomLineNumber));
        }

        return list;
    }

    [Fact]
    public void PlainStringIsNotCopied()
    {
        // A string without escape sequences must be returned as a slice of the source buffer.
        var utf8 = Encoding.UTF8.GetBytes("\"no escapes here\"");
        var reader = new TinyhandUtf8Reader(utf8);
        reader.Read().IsTrue();
        reader.AtomType.Is(TinyhandAtomType.Value_String);
        reader.ValueSpanToString.Is("no escapes here");
        reader.ValueSpan.Overlaps(utf8).IsTrue();
    }

    [Fact]
    public void EscapedString()
    {
        var atoms = ReadAll("\"a\\nb\\tc\\\"d\\\\e\\u0041\"");
        atoms.Count.Is(1);
        atoms[0].Type.Is(TinyhandAtomType.Value_String);
        atoms[0].Value.Is("a\nb\tc\"d\\eA");
    }

    [Fact]
    public void ManyEscapedStrings()
    {
        // The reader reuses one buffer for unescaping, so every value must still be
        // correct when it is copied out before the next Read().
        var sb = new StringBuilder();
        var expected = new List<string>();
        for (var i = 0; i < 50; i++)
        {
            sb.Append($"\"a\\n{new string('x', i)}\\tb\" ");
            expected.Add($"a\n{new string('x', i)}\tb");
        }

        var atoms = ReadAll(sb.ToString());
        atoms.Count.Is(expected.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            atoms[i].Value.Is(expected[i]);
        }
    }

    [Fact]
    public void TripleQuotedStringIsNotUnescaped()
    {
        var atoms = ReadAll("\"\"\"a\\nb\"\"\"");
        atoms.Count.Is(1);
        atoms[0].Value.Is("a\\nb");
    }

    [Fact]
    public void UnicodeLineSeparatorInWhitespace()
    {
        // U+2028 is a line separator. It must be skipped like any other white space
        // and must advance the line number.
        var atoms = ReadAll("a\u2028\u2028b");
        atoms.Count.Is(2);
        atoms[0].Value.Is("a");
        atoms[1].Value.Is("b");
        atoms[1].Line.Is(3);
    }

    [Fact]
    public void UnicodeSpacesAreSkipped()
    {
        // U+00A0, U+2002, U+3000 are white space.
        var atoms = ReadAll("a\u00a0b\u2002c\u3000d");
        atoms.Count.Is(4);
        atoms[0].Value.Is("a");
        atoms[1].Value.Is("b");
        atoms[2].Value.Is("c");
        atoms[3].Value.Is("d");
    }

    [Fact]
    public void MultiLineCommentCountsLineFeeds()
    {
        var atoms = ReadAll("a\n/* comment\nover\nthree lines */\nb");
        atoms.Count.Is(3);
        atoms[0].Value.Is("a");
        atoms[1].Type.Is(TinyhandAtomType.Comment);
        atoms[2].Value.Is("b");
        atoms[2].Line.Is(5);
    }

    [Fact]
    public void CommentAtEndOfFileKeepsItsText()
    {
        // A comment that is not terminated by a line feed must still report its text.
        var atoms = ReadAll("a // trailing", true);
        atoms[atoms.Count - 1].Type.Is(TinyhandAtomType.Comment);
        atoms[atoms.Count - 1].Value.Is("// trailing");

        var atoms2 = ReadAll("a # trailing", true);
        atoms2[atoms2.Count - 1].Type.Is(TinyhandAtomType.Comment);
        atoms2[atoms2.Count - 1].Value.Is("# trailing");
    }

    [Fact]
    public void Numbers()
    {
        var atoms = ReadAll("1 -2 3000000000 18446744073709551615 1.5 -2.5e3 0");
        atoms[0].Type.Is(TinyhandAtomType.Value_Long);
        atoms[2].Type.Is(TinyhandAtomType.Value_Long);
        atoms[3].Type.Is(TinyhandAtomType.Value_ULong);
        atoms[4].Type.Is(TinyhandAtomType.Value_Double);
        atoms[5].Type.Is(TinyhandAtomType.Value_Double);

        var utf8 = Encoding.UTF8.GetBytes("1 -2 3000000000 18446744073709551615 1.5 -2.5e3 0");
        var reader = new TinyhandUtf8Reader(utf8);
        reader.Read();
        reader.ValueLong.Is(1L);
        reader.Read();
        reader.ValueLong.Is(-2L);
        reader.Read();
        reader.ValueLong.Is(3000000000L);
        reader.Read();
        reader.ValueULong.Is(ulong.MaxValue);
        reader.Read();
        reader.ValueDouble.Is(1.5d);
        reader.Read();
        reader.ValueDouble.Is(-2500d);
        reader.Read();
        reader.ValueLong.Is(0L);
    }

    [Fact]
    public void MalformedNumberIsRejected()
    {
        // A token that looks like a number but is not fully parsable must not be
        // silently truncated to the part that happens to parse.
        Assert.ThrowsAny<Exception>(() => ReadAll("1.2.3"));
        Assert.ThrowsAny<Exception>(() => ReadAll("12+34"));
        Assert.ThrowsAny<Exception>(() => ReadAll("1-2"));
    }

    [Fact]
    public void SpecialDoubleValues()
    {
        var utf8 = Encoding.UTF8.GetBytes("double.NaN double.PositiveInfinity double.NegativeInfinity");
        var reader = new TinyhandUtf8Reader(utf8);
        reader.Read();
        double.IsNaN(reader.ValueDouble).IsTrue();
        reader.Read();
        reader.ValueDouble.Is(double.PositiveInfinity);
        reader.Read();
        reader.ValueDouble.Is(double.NegativeInfinity);
    }

    [Fact]
    public void Keywords()
    {
        var atoms = ReadAll("null true false");
        atoms[0].Type.Is(TinyhandAtomType.Value_Null);
        atoms[1].Type.Is(TinyhandAtomType.Value_True);
        atoms[2].Type.Is(TinyhandAtomType.Value_False);
    }

    [Fact]
    public void IdentifiersAndModifiers()
    {
        var atoms = ReadAll("abc @special &i32");
        atoms[0].Type.Is(TinyhandAtomType.Identifier);
        atoms[0].Value.Is("abc");
        atoms[1].Type.Is(TinyhandAtomType.SpecialIdentifier);
        atoms[1].Value.Is("special");
        atoms[2].Type.Is(TinyhandAtomType.Modifier);
    }

    [Fact]
    public void Binary()
    {
        var utf8 = Encoding.UTF8.GetBytes("b\"AQIDBA\"");
        var reader = new TinyhandUtf8Reader(utf8);
        reader.Read().IsTrue();
        reader.AtomType.Is(TinyhandAtomType.Value_Base64);
        reader.ValueBinary!.SequenceEqual(new byte[] { 1, 2, 3, 4, }).IsTrue();
    }

    [Fact]
    public void GroupsAndAssignment()
    {
        var atoms = ReadAll("a = { b = 1 }");
        atoms[0].Type.Is(TinyhandAtomType.Identifier);
        atoms[1].Type.Is(TinyhandAtomType.Assignment);
        atoms[2].Type.Is(TinyhandAtomType.StartGroup);
        atoms[6].Type.Is(TinyhandAtomType.EndGroup);
    }

    [Fact]
    public void ByteOrderMarkIsSkipped()
    {
        var utf8 = new byte[] { 0xEF, 0xBB, 0xBF, (byte)'a', };
        var reader = new TinyhandUtf8Reader(utf8);
        reader.Read().IsTrue();
        reader.ValueSpanToString.Is("a");
    }

    [Fact]
    public void EmptyInput()
    {
        ReadAll(string.Empty).Count.Is(0);
        ReadAll("   \n  ").Count.Is(0);
    }

    /// <summary>
    /// The reader must never read past the end of the buffer, whatever the input is.
    /// </summary>
    [Fact]
    public void TruncatedInputDoesNotReadPastTheEnd()
    {
        string[] sources =
        [
            "a\u2028b", "a\u00a0b", "a\u2002b", "a\u3000b",
            "/* comment */", "// comment\n", "# comment\n",
            "\"string\"", "\"\"\"literal\"\"\"", "b\"AQIDBA\"",
            "a = { b = [1, 2] }", "1.5e10", "&i32", "@id",
            "+ group", "\\", "\"\\u0041\"",
        ];

        foreach (var source in sources)
        {
            var utf8 = Encoding.UTF8.GetBytes(source);
            for (var length = 0; length <= utf8.Length; length++)
            {
                foreach (var contextual in new[] { false, true })
                {
                    try
                    {
                        var reader = new TinyhandUtf8Reader(utf8.AsSpan(0, length), contextual);
                        while (reader.Read())
                        {
                        }
                    }
                    catch (TinyhandException)
                    {// Expected for malformed input.
                    }
                }
            }
        }
    }
}
