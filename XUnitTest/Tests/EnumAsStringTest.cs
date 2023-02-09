// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

public enum TestEnum
{
    Default,
    Test,
}

[TinyhandObject(EnumAsString = true)]
public partial class EnumAsStringClass
{
    [Key(0)]
    public TestEnum X { get; set; }
}

[TinyhandObject]
public partial class EnumAsStringClass2
{
    [Key(0)]
    public string X { get; set; } = default!;
}

[TinyhandObject(EnumAsString = true)]
public partial class EnumAsStringClass3
{
    [KeyAsName]
    public TestEnum X { get; set; }
}

public class EnumAsStringClassTest
{
    [Fact]
    public void Test1()
    {
        var tc = new EnumAsStringClass();
        tc.X = TestEnum.Test;

        var td = new EnumAsStringClass2();
        td.X = TestEnum.Test.ToString();

        var st = TinyhandSerializer.SerializeToString(new EnumAsStringClass3());

        var tc2 = TinyhandSerializer.Deserialize<EnumAsStringClass>(TinyhandSerializer.Serialize(tc));
        tc2.IsStructuralEqual(tc);
        tc2 = TinyhandSerializer.DeserializeFromUtf8<EnumAsStringClass>(TinyhandSerializer.SerializeToUtf8(tc));
        tc2.IsStructuralEqual(tc);

        var td2 = TinyhandSerializer.Deserialize<EnumAsStringClass2>(TinyhandSerializer.Serialize(td));
        td2.IsStructuralEqual(td);
        td2 = TinyhandSerializer.DeserializeFromUtf8<EnumAsStringClass2>(TinyhandSerializer.SerializeToUtf8(td));
        td2.IsStructuralEqual(td);

        td2 = TinyhandSerializer.Deserialize<EnumAsStringClass2>(TinyhandSerializer.Serialize(tc));
        td2.IsStructuralEqual(td);
        td2 = TinyhandSerializer.DeserializeFromUtf8<EnumAsStringClass2>(TinyhandSerializer.SerializeToUtf8(tc));
        td2.IsStructuralEqual(td);

        tc2 = TinyhandSerializer.Deserialize<EnumAsStringClass>(TinyhandSerializer.Serialize(td));
        tc2.IsStructuralEqual(tc);
        tc2 = TinyhandSerializer.DeserializeFromUtf8<EnumAsStringClass>(TinyhandSerializer.SerializeToUtf8(td));
        tc2.IsStructuralEqual(tc);
    }
}
