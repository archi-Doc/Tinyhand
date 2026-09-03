// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Tinyhand;
using Tinyhand.Tree;
using Xunit;

namespace XUnitTest.Tests;

public class UnescapeTest
{
    private static string Unescape(string source)
    {
        var utf8 = Encoding.UTF8.GetBytes(source);
        return Encoding.UTF8.GetString(TinyhandHelper.GetUnescapedSpan(utf8));
    }

    [Fact]
    public void SimpleEscapes()
    {
        Unescape("abc").Is("abc");
        Unescape("a\\nb").Is("a\nb");
        Unescape("a\\tb").Is("a\tb");
        Unescape("a\\\\b").Is("a\\b");
        Unescape("a\\\"b").Is("a\"b");
        Unescape("a\\/b").Is("a/b");
    }

    [Fact]
    public void TrailingBackSlashDoesNotThrow()
    {
        // A source ending with a lone back slash must not read past the end.
        Unescape("abc\\").Is("abc");
        Unescape("\\").Is(string.Empty);
    }

    [Fact]
    public void UnicodeEscapeConsumesExactlyFourDigits()
    {
        Unescape("\\u0041").Is("A");

        // The four hex digits must not absorb the following characters, even when they are hex digits.
        Unescape("\\u0041BC").Is("ABC");
        Unescape("\\u0041\\u0042").Is("AB");
        Unescape("x\\u3042y").Is("xあy");

        // Surrogate pair.
        Unescape("\\uD83D\\uDE00").Is("\U0001F600");
    }

    [Fact]
    public void ParseStringWithUnicodeEscape()
    {
        var element = TinyhandParser.Parse("a = \"\\u0041BC\"");
        var group = (Group)element;
        var assignment = (Assignment)group.ElementList[0];
        ((Value_String)assignment.RightElement!).Utf16.Is("ABC");
    }
}
