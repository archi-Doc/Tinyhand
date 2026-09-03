// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class Utf8ToBinaryTest
{
    private static readonly string[] Texts =
    [
        "{a = 1, b = \"x\"}",
        "a = 1\nb = 2\nc = {d = 3}",
        "{\n  a = 1 // comment\n  b = 2 /* multi\nline */\n  c = 3 # hash\n}",
        "{1, 2, 3}",
        "1, 2, 3",
        "{}",
        "",
        "   \n\n  ",
        "{{}}",
        "{{{1}}}",
        "{{1, 2}, {3}, {}}",
        "a = {b = {c = {d = 1}}}",
        "+ 1\n+ 2\n+ 3",
        "a = \n+ 1\n+ 2",
        "x = 1\n  y = 2\n    z = 3\nw = 4",
        "obj\n  a = 1\n  b = 2\nobj2\n  c = 3",
        "null, true, false, 1, -1, 1.5, 1e5, -2.5e-3, 18446744073709551615, -9223372036854775808, 9223372036854775807, 1234567890123456789, 12345678901234567890",
        "\"esc\\\"aped\\n\\t\\\\\\u0041\", 'single', \"\"\"triple\nquoted\"\"\", \"\", \"\"\"\"\"\"",
        "b\"AQID\", b'BAUG', b\"\"",
        "@special = 1, &i32 x = 2, &required z = 4",
        "@special = 1, &i32 x = 2, &key(1) y = 3",
        "ident, ident2 = 3; a; b",
        "double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.X",
        "{a = {}, b = {{}}, c = {{}, {}}}",
        "{a = 1}\n{b = 2}",
        "\uFEFF{bom = 1}",
        "{a\u00A0=\u30001}",
        "{a = 1,\u2028b = 2}",
        "{a = 1,\u2029b = 2}",
        "{a = 1, b\u2000=\u200A2}",
        "{ a = { b = 1 } c = 2 }",
        "123abc",
        "1.2.3",
        "+",
        "-",
        "+-1",
        "{a = 1",
        "a = 1}",
        "{\"unterminated",
        "{\"\"\"unterminated",
        "{'unterminated",
        "{a = b\"!!!\"}",
        "{a = b\"unterminated",
        "\"control\u0001char\"",
        "\"\"\"control\u0001char\"\"\"",
        "a\n  = 1",
        "{\r\n  a = 1\r\n  b = {\r\n    c = 2\r\n  }\r\n}\r\n",
        "a = 1\n\n\n  b = 2",
        "x\n  + 1\n  + 2\ny\n  + 3",
        "a = 1 /x",
        "a = 1 /",
        "a = 1 #",
        "a = 1 // comment",
        "a = 1 /* comment",
        "/* a\nb */ c = 1",
        "// a\n  b = 1",
        "&",
        "&unknown",
        "@",
        "@1abc",
        "@a1",
        "(",
        "a(",
        "a = ) b",
        "'",
        "\"",
        "b\"",
        "b'A",
        "\u0001abc",
        "1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18",
        "a=1 b=2 c=3 d=4 e=5 f=6 g=7 h=8 i=9 j=10 k=11 l=12 m=13 n=14 o=15 p=16 q=17",
        "{a=1, b=2, c=3, d=4, e=5, f=6, g=7, h=8, i=9, j=10, k=11, l=12, m=13, n=14, o=15, p=16, q=17}",
        "a\n  b\n    c\n      d\n  e",
        "a\n    b",
        "a\n  b\n c",
        "a\n   b",
    ];

    private static readonly string[] Words = ["abc", "a b", "null", "true", "12", "", "x\"y", "日本", "a=b", "{c}", "&i32", "@id", "b\"aa\"", "-1.5", "e", "1e5", "tab\t", "lf\n", "//c", "#h", "a,b", "'q'"];

    [Fact]
    public void HandWritten()
    {
        foreach (var text in Texts)
        {
            Compare(Encoding.UTF8.GetBytes(text));
        }
    }

    [Fact]
    public void LargeStrings()
    {
        foreach (var length in new[] { 31, 32, 255, 256, 65535, 65536 })
        {
            var s = new string('a', length);
            Compare(Encoding.UTF8.GetBytes($"{{a = \"{s}\", b = \"\\t{s}\", c = \"\"\"{s}\"\"\", d = b\"{Arc.Crypto.Base64Url.EncodeToString(new byte[length])}\"}}"));
        }

        // A string with escapes that drops to a smaller header (32 escaped bytes -> 16 bytes).
        Compare(Encoding.UTF8.GetBytes("\"" + string.Concat(System.Linq.Enumerable.Repeat("\\n", 16)) + "\""));
    }

    [Fact]
    public void RandomTrees()
    {
        foreach (var option in new[] { TinyhandSerializerOptions.ConvertToString, TinyhandSerializerOptions.ConvertToSimpleString, TinyhandSerializerOptions.ConvertToStrictString })
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var r = new Random(seed);
                var w = new TinyhandWriter(new byte[256]);
                var n = r.Next(1, 4);
                if (r.Next(2) == 0)
                {
                    w.WriteArrayHeader(n);
                    for (var i = 0; i < n; i++)
                    {
                        GenerateTree(ref w, r, 1, false);
                    }
                }
                else
                {
                    w.WriteMapHeader(n);
                    for (var i = 0; i < n; i++)
                    {
                        GenerateTree(ref w, r, 1, true);
                        GenerateTree(ref w, r, 1, false);
                    }
                }

                var binary = w.FlushAndGetArray();
                w.Dispose();

                foreach (var omit in new[] { false, true })
                {
                    var rawWriter = new Arc.IO.TinyhandRawWriter(new byte[256]);
                    TinyhandTreeConverter.FromBinaryToUtf8(binary, ref rawWriter, option, omit);
                    var utf8 = rawWriter.FlushAndGetArray();
                    rawWriter.Dispose();
                    Compare(utf8);
                }
            }
        }
    }

    private static void GenerateTree(ref TinyhandWriter w, Random r, int depth, bool key)
    {
        int k = key ? 5 : r.Next(depth >= 5 ? 6 : 10);
        switch (k)
        {
            case 0: w.Write(r.Next(-1000, 1000)); break;
            case 1: w.Write(r.NextDouble() * 100); break;
            case 2: w.Write(r.Next(2) == 0); break;
            case 3: w.WriteNil(); break;
            case 4: w.Write((ulong)r.NextInt64()); break;
            case 5: w.WriteString(Encoding.UTF8.GetBytes(Words[r.Next(Words.Length)])); break;
            case 6:
            case 8:
                {
                    var n = r.Next(0, 20);
                    w.WriteArrayHeader(n);
                    for (var i = 0; i < n; i++)
                    {
                        GenerateTree(ref w, r, depth + 1, false);
                    }

                    break;
                }

            case 7:
            case 9:
                {
                    var n = r.Next(0, 4);
                    w.WriteMapHeader(n);
                    for (var i = 0; i < n; i++)
                    {
                        GenerateTree(ref w, r, depth + 1, true);
                        GenerateTree(ref w, r, depth + 1, false);
                    }

                    break;
                }
        }
    }

    private static void Compare(byte[] utf8)
    {
        foreach (var omit in new[] { false, true })
        {
            var fast = Run(utf8, omit, false);
            var reader = Run(utf8, omit, true);
            Assert.Equal(reader, fast);
        }
    }

    private static string Run(byte[] utf8, bool omit, bool withReader)
    {
        var buffer = TinyhandTreeConverter.BinaryBuffer.Acquire();
        try
        {
            if (withReader)
            {
                TinyhandTreeConverter.FromUtf8ToBinaryWithReader(utf8, omit, ref buffer, null);
            }
            else
            {
                TinyhandTreeConverter.FromUtf8ToBinary(utf8, omit, ref buffer);
            }

            return Convert.ToHexString(buffer.Span);
        }
        catch (Exception ex)
        {
            return ex.GetType().Name + ": " + ex.Message;
        }
        finally
        {
            buffer.Release();
        }
    }
}
