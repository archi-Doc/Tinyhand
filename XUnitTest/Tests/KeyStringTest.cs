// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandGenerateHash("..\\Resources\\strings.tinyhand")]
[TinyhandGenerateHash("..\\Resources\\strings2.tinyhand")]
public static partial class Hashed
{
}

public class KeyStringTest
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

        var ks = new HashedString();
        ks.LoadStream("en", ms1);
        ms1.Position = 0;

        ks.Get("a").Is("A");
        ks.Get("b").Is("BB");
        ks.Get("c").Is("CCC");
        ks.Get("d").Is("DDDD");
        ks.Get("e").Is(ks.ErrorMessage);

        ks.LoadStream("en", ms2, true);
        ms2.Position = 0;

        ks.Get("a").Is("A");
        ks.Get("b").Is(ks.ErrorMessage);
        ks.Get("c").Is("111");
        ks.Get("d").Is(ks.ErrorMessage);
        ks.Get("e").Is("22222");

        ks.LoadStream("en", ms1, true);
        ms1.Position = 0;
        ks.LoadStream("en", ms2);
        ms2.Position = 0;

        ks.Get("a").Is("A");
        ks.Get("b").Is("BB");
        ks.Get("c").Is("CCC"); // Not overwritten
        ks.Get("d").Is("DDDD");
        ks.Get("e").Is("22222");

        ks.LoadStream("ja", ms3);
        ms3.Position = 0;
        ks.ChangeCulture("ja");

        ks.Get("a").Is("あ"); // Overwritten
        ks.Get("b").Is("BB");
        ks.Get("c").Is("CCC");
        ks.Get("d").Is("DDDD");
        ks.Get("e").Is("22222");
    }

    [Fact]
    public void Test2()
    {
        var hs = new Tinyhand.HashedString();
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        hs.LoadAssembly(null, asm, "Resources.strings.tinyhand");
        hs.LoadAssembly(null, asm, "Resources.strings2.tinyhand");

        hs.Get(Hashed.NameA).Is("a");
        hs.Get(Hashed.NameA).IsNot("A");
        hs.Get(Hashed.NameB).Is("b");
        hs.Get(Hashed.NameD).Is("d");
        hs.Get(Hashed.GroupA.NameX).Is("X");
        hs.Get(Hashed.GroupA.NameY).Is("Y");

        hs.Get("NameA").Is("a");
        hs.Get("Namea").IsNot("a");
        hs.Get("GroupA.NameX").Is("X");
    }
}
