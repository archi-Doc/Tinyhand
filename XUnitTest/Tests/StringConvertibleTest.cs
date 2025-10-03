// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc;
using Arc.Crypto;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class StringConvertibleTestClass : IStringConvertible<StringConvertibleTestClass>, IEquatable<StringConvertibleTestClass>
{// @Base64.Url(Byte16)
    public static int MaxStringLength
        => 23;

    [Key(0)]
    public byte[] Byte16 { get; set; } = [];

    public static bool TryParse(ReadOnlySpan<char> source, out StringConvertibleTestClass? instance, out int read, IConversionOptions? conversionOptions = default)
    {
        instance = null;
        read = 0;
        if (source.Length < MaxStringLength ||
            source[0] != '@')
        {
            return false;
        }

        source = source.Slice(1);
        var b = Base64.Url.FromStringToByteArray(source);
        if (b.Length != 16)
        {
            return false;
        }

        instance = new();
        instance.Byte16 = b;
        read = source.Length + 1;
        return true;
    }

    public bool Equals(StringConvertibleTestClass? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Byte16.SequenceEqual(other.Byte16);
    }

    int IStringConvertible<StringConvertibleTestClass>.GetStringLength()
        => MaxStringLength;

    bool IStringConvertible<StringConvertibleTestClass>.TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions)
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
    [Key("Class1")]
    public StringConvertibleTestClass Class1 { get; set; } = new();

    [KeyAsName]
    public StringConvertibleTestClass? Class2 { get; set; } = new();

    // [KeyAsName(ConvertToString = true)]
    // TestRecord Class3 { get; set; } = new();
}

[TinyhandObject]
public partial class StringConvertibleTestClass3
{
    [Key(0)]
    public StringConvertibleTestClass[] Array { get; set; } = [];

    [Key(1)]
    public List<StringConvertibleTestClass> List { get; set; } = new();
}

public class StringConvertibleTest
{
    [Fact]
    public void Test1()
    {
        var tc = new StringConvertibleTestClass();
        tc.Byte16 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,];
        var st = tc.ConvertToString();
        StringConvertibleTestClass.TryParse(st, out var tc2, out var read);
        read.Is(st.Length);

        var tc3 = new StringConvertibleTestClass2();
        tc3.Class1 = tc2!;
        tc3.Class2 = tc2!;
        st = TinyhandSerializer.SerializeToString(tc3);
        var list = st.Replace("\r\n", "\n").Split(['\n', '\r',]);
        list[0].Is("Class1=\"@AQIDBAUGBwgJCgsMDQ4PEA\"");
        list[1].Is("Class2=\"@AQIDBAUGBwgJCgsMDQ4PEA\"");

        var tc4 = TinyhandSerializer.DeserializeFromString<StringConvertibleTestClass2>(st);
        tc4.Class1.Byte16.SequenceEqual(tc3.Class1.Byte16).IsTrue();
        tc4.Class2.Byte16.SequenceEqual(tc3.Class2.Byte16).IsTrue();

        var tc5 = new StringConvertibleTestClass3();
        tc5.Array = [tc, tc2!,];
        tc5.List = [tc, tc2!,];
        st = TinyhandSerializer.SerializeToString(tc5, TinyhandSerializerOptions.ConvertToSimpoleString);
        var tc6 = TinyhandSerializer.DeserializeFromString<StringConvertibleTestClass3>(st);
        tc6.Array.SequenceEqual(tc5.Array).IsTrue();
        tc6.List.SequenceEqual(tc5.List).IsTrue();
    }
}
