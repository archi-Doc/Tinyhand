﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

public class KeyStringTest
{
    public string Data1 = "a = \"A\", B = \"BB\", c = \"CCC\", D = \"DDDD\"";
    public string Data2 = "a = \"A\", b = null, c = \"111\", E = \"22222\"";
    public string Data3 = "a = \"あ\", E = null";

    [Fact]
    public void Test1()
    {
        using var ms1 = new MemoryStream(Encoding.UTF8.GetBytes(Data1));
        using var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(Data2));
        using var ms3 = new MemoryStream(Encoding.UTF8.GetBytes(Data3));

        var ks = new KeyString();
        ks.LoadStream("en", ms1);
        ms1.Position = 0;

        ks.Get("a").Is("A");
        ks.Get("b").Is("BB");
        ks.Get("c").Is("CCC");
        ks.Get("d").Is("DDDD");
        ks.Get("e").Is(ks.ErrorMessage);

        ks.LoadStream("en", ms2, true);
        ms2.Position = 0;

        ks.Get("A").Is("A");
        ks.Get("B").Is(ks.ErrorMessage);
        ks.Get("C").Is("111");
        ks.Get("D").Is(ks.ErrorMessage);
        ks.Get("E").Is("22222");

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
}
