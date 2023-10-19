// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class Utf8StringClass
{
    public Utf8StringClass()
    {
        this.ByteArray = Encoding.UTF8.GetBytes("Test16");
        this.Utf8Array = new Utf8String[2];
        this.Utf8Array[0] = new("abc"u8);
        this.Utf8Array[1] = new("efg"u8);
    }

    [Key(0)]
    public string Utf16String { get; set; } = "Test16";

    [Key(1)]
    public byte[] ByteArray { get; set; }

    [Key(2)]
    public Utf8String Utf8 { get; set; } = new("test"u8);

    [Key(3)]
    public Utf8String? NullableUtf8 { get; set; } = new("test2"u8);

    [Key(4)]
    public Utf8String[] Utf8Array { get; set; }
}

public class PropertyAccessibilityTest
{
    [Fact]
    public void Test1()
    {
        var t = new Utf8StringClass();
        var st = TinyhandSerializer.SerializeToString(t);

        var t2 = TinyhandSerializer.DeserializeFromString<Utf8StringClass>(st);

        t.IsStructuralEqual(t2);
    }
}
