// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using Tinyhand;
using Tinyhand.Tests;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject(AddAlternateKey = true)]
public partial class DualKeyTestClass : IEquatable<DualKeyTestClass>
{
    [Key(0)]
    public int A { get; set; }

    [Key(1)]
    public string B { get; set; } = "Test";

    [Key(2)]
    public StringConvertibleTestClass Class1 { get; set; } = new();

    public DualKeyTestClass()
    {
    }

    public bool Equals(DualKeyTestClass? other)
    {
        if (other is null)
        {
            return false;
        }

        return this.A == other.A && this.B == other.B;
    }
}

public class DualKeyTest
{
    [Fact]
    public void Test1()
    {
        var td = new StringConvertibleTestClass();
        td.Byte16 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,];
        var tc = new DualKeyTestClass() { A = 123, B = "Hello", Class1 = td, };

        var bin = TinyhandSerializer.Serialize(tc);
        var tc2 = TinyhandSerializer.Deserialize<DualKeyTestClass>(bin);
        tc2.Equals(tc).IsTrue();

        var st = TinyhandSerializer.SerializeToString(tc);
        tc2 = TinyhandSerializer.DeserializeFromString<DualKeyTestClass>(st);
        tc2.Equals(tc).IsTrue();

        tc = new() { A = 1 };
        tc2 = TinyhandSerializer.DeserializeFromString<DualKeyTestClass>("A = 1,Class1=\"@AQIDBAUGBwgJCgsMDQ4PEA\"");
        tc2.Equals(tc).IsTrue();

        var strictOption = TinyhandSerializerOptions.ConvertToString with { Compose = TinyhandComposeOption.Strict, };
        tc2 = TinyhandSerializer.DeserializeFromString<DualKeyTestClass>("{A = 1, Class1=\"@AQIDBAUGBwgJCgsMDQ4PEA\"}", strictOption);
        tc2.Equals(tc).IsTrue();
    }
}
