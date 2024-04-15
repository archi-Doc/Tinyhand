// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Arc.Crypto;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class StringConvertibleTestClass : IStringConvertible<StringConvertibleTestClass>
{// @Base64.Url(Byte16)
    public static int MaxStringLength
        => 23;

    [Key(0)]
    public byte[] Byte16 { get; set; } = [];

    public static bool TryParse(ReadOnlySpan<char> source, out StringConvertibleTestClass? instance)
    {
        if (source.Length < MaxStringLength ||
            source[0] != '@')
        {
            instance = null;
            return false;
        }

        source = source.Slice(1);
        var b = Base64.Url.FromStringToByteArray(source);
        if (b.Length != 16)
        {
            instance = null;
            return false;
        }

        instance = new();
        instance.Byte16 = b;
        return true;
    }

    int IStringConvertible<StringConvertibleTestClass>.GetStringLength()
        => MaxStringLength;

    bool IStringConvertible<StringConvertibleTestClass>.TryFormat(Span<char> destination, out int written)
    {
        if (destination.Length < MaxStringLength)
        {
            written = 0;
            return false;
        }

        destination[0] = '@';
        destination = destination.Slice(1);
        if (!Base64.Url.FromByteArrayToSpan(this.Byte16, destination, out written))
        {
            written = 0;
            return false;
        }

        written++;
        return true;
    }
}

[TinyhandObject]
public partial class StringConvertibleTestClass2
{
    [Key("Class1", ConvertToString = true)]
    public StringConvertibleTestClass Class1 { get; set; } = new();

    [KeyAsName(ConvertToString = true)]
    public StringConvertibleTestClass? Class2 { get; set; } = new();

    // [KeyAsName(ConvertToString = true)]
    // TestRecord Class3 { get; set; } = new();
}

public class StringConvertibleTest
{
    [Fact]
    public void Test1()
    {
        var tc = new StringConvertibleTestClass();
        tc.Byte16 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,];
        var st = tc.ConvertToString();
        StringConvertibleTestClass.TryParse(st, out var tc2);

        var tc3 = new StringConvertibleTestClass2();
        tc3.Class1 = tc2!;
        tc3.Class2 = tc2!;
        st = TinyhandSerializer.SerializeToString(tc3);
        var list = st.Replace("\r\n", "\n").Split(['\n', '\r',]);
        list[0].Is("Class1 = \"@AQIDBAUGBwgJCgsMDQ4PEA\"");
        list[1].Is("Class2 = \"@AQIDBAUGBwgJCgsMDQ4PEA\"");

        var tc4 = TinyhandSerializer.DeserializeFromString<StringConvertibleTestClass2>(st);
        tc4.Class1.Byte16.SequenceEqual(tc3.Class1.Byte16).IsTrue();
        tc4.Class2.Byte16.SequenceEqual(tc3.Class2.Byte16).IsTrue();
    }
}
