// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject(DualKey = true)]
public partial class DualKeyTestClass : IEquatable<DualKeyTestClass>
{
    [Key(0)]
    public int A { get; set; }

    [Key(1)]
    public string B { get; set; } = "Test";

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
        var tc = new DualKeyTestClass() { A = 123, B = "Hello" };

        var bin = TinyhandSerializer.Serialize(tc);
        var tc2 = TinyhandSerializer.Deserialize<DualKeyTestClass>(bin);
        tc2.Equals(tc).IsTrue();

        var st = TinyhandSerializer.SerializeToString(tc);
        tc2 = TinyhandSerializer.DeserializeFromString<DualKeyTestClass>(st);
        tc2.Equals(tc).IsTrue();

        tc = new() { A = 1 };
        tc2 = TinyhandSerializer.DeserializeFromString<DualKeyTestClass>("A = 1");
        tc2.Equals(tc).IsTrue();

        var strictOption = TinyhandSerializerOptions.ConvertToString with { Compose = TinyhandComposeOption.Strict, };
        tc2 = TinyhandSerializer.DeserializeFromString<DualKeyTestClass>("{A = 1}", strictOption);
        tc2.Equals(tc).IsTrue();
    }
}
