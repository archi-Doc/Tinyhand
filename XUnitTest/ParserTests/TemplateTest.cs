// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Tinyhand;
using Tinyhand.Tree;
using Xunit;

namespace XUnitTest.ParserTests;

public class BasicTest
{
    [Fact]
    public void Test1()
    {
        var st = "0, abc; -123 1.2";
        var e = TinyhandParser.Parse(st);

        var g = (Group)e;
        g.ElementList.Count.Is(4);
        ((Value_Long)g.ElementList[0]).ValueLong.Is(0);
        ((Value_Identifier)g.ElementList[1]).IdentifierUtf16.Is("abc");
        ((Value_Long)g.ElementList[2]).ValueLong.Is(-123);
        ((Value_Double)g.ElementList[3]).ValueDouble.Is(1.2);

        st = TinyhandComposer.ComposeToString(e);

        st = "&i32, &key";
        e = TinyhandParser.Parse(st);
        g = (Group)e;
        g.ElementList.Count.Is(2);
        ((Modifier)g.ElementList[0]).ModifierUtf16.Is("i32");
        ((Modifier)g.ElementList[1]).ModifierUtf16.Is("key");

        var st2 = TinyhandComposer.ComposeToString(e, TinyhandComposeOption.Simple);
        st2.Is(st);
    }
}
