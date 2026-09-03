// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using System.Text;
using Tinyhand;
using Tinyhand.Tree;
using Xunit;

namespace XUnitTest.Tests;

public class ComposerBoundaryTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(2048)]
    public void AllAsciiControlCharactersRoundTrip(int repeat)
    {
        var value = "prefix" + new string(Enumerable.Range(0, 32 * repeat).Select(x => (char)(x % 32)).ToArray()) + "日本語\"\\suffix";
        var bytes = TinyhandComposer.Compose(new Value_String(value));
        var parsed = (Group)TinyhandParser.Parse(bytes);
        Assert.Equal(value, Assert.IsType<Value_String>(Assert.Single(parsed.ElementList)).Utf16);
        Assert.Equal(value, TinyhandSerializer.DeserializeFromString<string>(TinyhandSerializer.SerializeToString(value)));
    }

    [Theory]
    [InlineData(TinyhandComposeOption.Standard)]
    [InlineData(TinyhandComposeOption.UseContextualInformation)]
    [InlineData(TinyhandComposeOption.Simple)]
    [InlineData(TinyhandComposeOption.Strict)]
    public void NestedGroupsAndAdjacentValuesRoundTrip(TinyhandComposeOption option)
    {
        var source = TinyhandParser.Parse("a = 1 b = 2 nested = { c = 3 deeper = { d = 4 } } e = 5"u8, TinyhandParserOptions.ContextualInformation);
        var composed = TinyhandComposer.Compose(source, option);
        var expected = TinyhandComposer.Compose(source, TinyhandComposeOption.Simple);
        var parsed = TinyhandParser.Parse(composed);
        Assert.Equal(Encoding.UTF8.GetString(expected), TinyhandComposer.ComposeToString(parsed, TinyhandComposeOption.Simple));
    }

    [Fact]
    public void ContextualCompositionPreservesCommentsAndNestedGroups()
    {
        var source = TinyhandParser.Parse("""
            // header
            a = 1 // first
            nested = { // inside
              b = 2
              c = "three" // last
            }
            e = 5
            """, TinyhandParserOptions.ContextualInformation);
        var composed = TinyhandComposer.ComposeToString(source, TinyhandComposeOption.UseContextualInformation);
        foreach (var comment in new[] { "// header", "// first", "// inside", "// last" })
        {
            Assert.Contains(comment, composed);
        }

        Assert.Equal(TinyhandComposer.ComposeToString(source, TinyhandComposeOption.Simple),
            TinyhandComposer.ComposeToString(TinyhandParser.Parse(composed), TinyhandComposeOption.Simple));
    }
}
