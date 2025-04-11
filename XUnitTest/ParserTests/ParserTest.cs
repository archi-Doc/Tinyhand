// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.Tree;
using Xunit;

#pragma warning disable CS0168 // Variable is declared but never used

namespace XUnitTest.ParserTests;

public class ParserTest
{
    [Fact]
    public void Test0()
    {
        Element e;
        Group g, g2;
        Assignment a;
        Value_Identifier i;
    }

    [Fact]
    public void Test1()
    {
        Element e;
        Group g, g2, r;
        Assignment a;
        Value_Identifier i;

        e = TinyhandParser.Parse(string.Empty);
        g = (Group)e;
        g.ElementList.Count.Is(0);

        e = TinyhandParser.Parse("a=b");
        g = (Group)e;
        g.ElementList.Count.Is(1);
        a = (Assignment)g.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("a");
        ((Value_Identifier)a.RightElement!).Utf16.Is("b");

        e = TinyhandParser.Parse("\"a\"='x'");
        g = (Group)e;
        g.ElementList.Count.Is(1);
        a = (Assignment)g.ElementList[0];
        ((Value_String)a.LeftElement!).Utf16.Is("a");
        ((Value_String)a.RightElement!).Utf16.Is("x");

        e = TinyhandParser.Parse("a={ b= 12}");
        g = (Group)e;
        g.ElementList.Count.Is(1);
        a = (Assignment)g.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("a");
        g2 = (Group)a.RightElement!;
        g2.ElementList.Count.Is(1);
        a = (Assignment)g2.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("b");
        ((Value_Long)a.RightElement!).ValueLong.Is(12);

        e = TinyhandParser.Parse("""
            a= // Comment
              b= 12
            """);
        g = (Group)e;
        g.ElementList.Count.Is(1);
        a = (Assignment)g.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("a");
        g2 = (Group)a.RightElement!;
        g2.ElementList.Count.Is(1);
        a = (Assignment)g2.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("b");
        ((Value_Long)a.RightElement!).ValueLong.Is(12);

        e = TinyhandParser.Parse("""
            a= {// Comment
              b= 12/*Comment*/
                c = 'z'#Comment
            }
            """);
        g = (Group)e;
        g.ElementList.Count.Is(1);
        a = (Assignment)g.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("a");
        g2 = (Group)a.RightElement!;
        g2.ElementList.Count.Is(2);
        a = (Assignment)g2.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("b");
        ((Value_Long)a.RightElement!).ValueLong.Is(12);
        g2 = (Group)g2.ElementList[1];
        a = (Assignment)g2.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("c");
        ((Value_String)a.RightElement!).Utf16.Is("z");

        e = TinyhandParser.Parse("""
            root = 
              a={1,2 ,b="c",}
              a={
                12,d
                'z'#Comment
                  b=1.23 // Comment
                  c=abc}
            root2=
              {
                a=1
              }
            root3=
            {
              b=2
              x=
                y=1
                y='a'
                }
            """);
        g = (Group)e;
        g.ElementList.Count.Is(3);
        a = (Assignment)g.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("root");
        r = (Group)a.RightElement!;
        r.ElementList.Count.Is(2);
        a = (Assignment)r.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("a");
        g2 = (Group)a.RightElement!; // {1,2 ,b="c",}
        g2.ElementList.Count.Is(3);
        ((Value_Long)g2.ElementList[0]).ValueLong.Is(1);
        ((Value_Long)g2.ElementList[1]).ValueLong.Is(2);
        a = (Assignment)g2.ElementList[2];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("b");
        ((Value_String)a.RightElement!).Utf16.Is("c");
        a = (Assignment)r.ElementList[1];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("a");
        g2 = (Group)a.RightElement!; // { 12, d, 'z', {b = 1.23, c = abc}}
        g2.ElementList.Count.Is(4);
        ((Value_Long)g2.ElementList[0]).ValueLong.Is(12);
        ((Value_Identifier)g2.ElementList[1]).Utf16.Is("d");
        ((Value_String)g2.ElementList[2]).Utf16.Is("z");
        g2 = (Group)g2.ElementList[3];
        g2.ElementList.Count.Is(2);
        a = (Assignment)g2.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("b");
        ((Value_Double)a.RightElement!).ValueDouble.Is(1.23);
        a = (Assignment)g2.ElementList[1];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("c");
        ((Value_Identifier)a.RightElement!).Utf16.Is("abc");
        a = (Assignment)g.ElementList[1]; // root2
        ((Value_Identifier)a.LeftElement!).Utf16.Is("root2");
        g2 = (Group)a.RightElement!;
        g2.ElementList.Count.Is(1);
        g2 = (Group)g2.ElementList[0]!;
        g2.ElementList.Count.Is(1);
        a = (Assignment)g2.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("a");
        ((Value_Long)a.RightElement!).ValueLong.Is(1);
        a = (Assignment)g.ElementList[2]; // root3
        ((Value_Identifier)a.LeftElement!).Utf16.Is("root3");
        g2 = (Group)a.RightElement!;
        g2.ElementList.Count.Is(1);
        a = (Assignment)g2.ElementList[0];
        ((Value_Identifier)a.LeftElement!).Utf16.Is("b");
        ((Value_Long)a.RightElement!).ValueLong.Is(2);
    }

    [Fact]
    public void TestException()
    {
        Assert.Throws<TinyhandException>(() => TinyhandParser.Parse("a={ b= 12 "));

        Assert.Throws<TinyhandException>(() => TinyhandParser.Parse("""
            a=
             b= 12
            """));

        Assert.Throws<TinyhandException>(() => TinyhandParser.Parse("""
            root = 
              a={1,2 ,b="c",}
              a={
              12,
              'z'#Comment
                b=1.23 // Comment
                c=abc}
            root2=
              {
              a=1
            }
            """));
    }

    [Fact]
    public void Test()
    {
        var st = "0, abc; -123 1.2";
        var e = TinyhandParser.Parse(st);

        var g = (Group)e;
        g.ElementList.Count.Is(4);
        ((Value_Long)g.ElementList[0]).ValueLong.Is(0);
        ((Value_Identifier)g.ElementList[1]).Utf16.Is("abc");
        ((Value_Long)g.ElementList[2]).ValueLong.Is(-123);
        ((Value_Double)g.ElementList[3]).ValueDouble.Is(1.2);

        st = TinyhandComposer.ComposeToString(e);

        st = "&i32, &key";
        e = TinyhandParser.Parse(st);
        g = (Group)e;
        g.ElementList.Count.Is(2);
        ((Modifier)g.ElementList[0]).Utf16.Is("i32");
        ((Modifier)g.ElementList[1]).Utf16.Is("key");

        var st2 = TinyhandComposer.ComposeToString(e, TinyhandComposeOption.Simple);
        st2.Is(st);
    }
}
