// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using System.Text;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandGenerateHash("..\\Resources\\strings.tinyhand")]
[TinyhandGenerateHash("../Resources/strings2.tinyhand")]
public static partial class Hashed
{
}

public class HashedStringTest
{
    public string Data1 = "a = \"A\", b = \"BB\", c = \"CCC\", d = \"DDDD\"";
    public string Data2 = "a = \"A\", b = null, c = \"111\", e = \"22222\"";
    public string Data3 = "a = \"あ\", e = null";

    [Fact]
    public void Test1()
    {
        using var ms1 = new MemoryStream(Encoding.UTF8.GetBytes(Data1));
        using var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(Data2));
        using var ms3 = new MemoryStream(Encoding.UTF8.GetBytes(Data3));

        HashedString.LoadStream("en", ms1);
        ms1.Position = 0;

        HashedString.Get("a").Is("A");
        HashedString.Get("b").Is("BB");
        HashedString.Get("c").Is("CCC");
        HashedString.Get("d").Is("DDDD");
        HashedString.Get("e").Is(HashedString.ErrorMessage);

        HashedString.LoadStream("en", ms2, true);
        ms2.Position = 0;

        HashedString.Get("a").Is("A");
        HashedString.Get("b").Is(HashedString.ErrorMessage);
        HashedString.Get("c").Is("111");
        HashedString.Get("d").Is(HashedString.ErrorMessage);
        HashedString.Get("e").Is("22222");

        HashedString.LoadStream("en", ms1, true);
        ms1.Position = 0;
        HashedString.LoadStream("en", ms2);
        ms2.Position = 0;

        HashedString.Get("a").Is("A");
        HashedString.Get("b").Is("BB");
        HashedString.Get("c").Is("CCC"); // Not overwritten
        HashedString.Get("d").Is("DDDD");
        HashedString.Get("e").Is("22222");

        HashedString.LoadStream("ja", ms3);
        ms3.Position = 0;
        HashedString.ChangeCulture("ja");

        HashedString.Get("a").Is("あ"); // Overwritten
        HashedString.Get("b").Is("BB");
        HashedString.Get("c").Is("CCC");
        HashedString.Get("d").Is("DDDD");
        HashedString.Get("e").Is("22222");
    }

    [Fact]
    public void Test2()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        HashedString.LoadAssembly(null, asm, "Resources.strings.tinyhand");
        HashedString.LoadAssembly(null, asm, "Resources.strings2.tinyhand");

        HashedString.Get(Hashed.NameA).Is("a");
        HashedString.Get(Hashed.NameA).IsNot("A");
        HashedString.Get(Hashed.NameB).Is("b");
        HashedString.Get(Hashed.NameD).Is("d");
        HashedString.Get(Hashed.GroupA.NameX).Is("X");
        HashedString.Get(Hashed.GroupA.NameY).Is("Y");

        HashedString.Get("NameA").Is("a");
        HashedString.Get("Namea").IsNot("a");
        HashedString.Get("GroupA.NameX").Is("X");
    }
}
