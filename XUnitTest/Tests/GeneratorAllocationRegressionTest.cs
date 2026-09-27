// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand.Generator;
using Xunit;

namespace XUnitTest.Tests;

public class GeneratorAllocationRegressionTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("one", "    one\r\n")]
    [InlineData("one\r\ntwo\nthree\rfour", "    one\r\n    two\r\n    three\r\n    four\r\n")]
    [InlineData("one\r\n", "    one\r\n")]
    [InlineData("\r\n\r\n", "    \r\n    \r\n")]
    [InlineData("\n\r", "    \r\n    \r\n")]
    public void AppendingBlocksPreservesLineEndingsAndIndentation(string input, string expected)
    {
        var block = new ScopingStringBuilder();
        block.Append(input, false);
        var builder = new ScopingStringBuilder();
        using (builder.ScopeBrace("class Sample"))
        {
            builder.Append(block);
        }

        Assert.Equal("class Sample\r\n{\r\n" + expected + "}\r\n", builder.Finalize());
        Assert.Equal(input, block.Finalize());
    }

    [Fact]
    public void AppendingSelfUsesOriginalContentOnly()
    {
        var builder = new ScopingStringBuilder();
        builder.Append("one\ntwo", false);
        builder.Append(builder);
        Assert.Equal("one\ntwoone\r\ntwo\r\n", builder.Finalize());
    }

    [Fact]
    public void FinalizePrependsHeadersClosesScopesAndResetsForReuse()
    {
        var builder = new ScopingStringBuilder();
        builder.AddHeader("// generated");
        builder.AddUsing("Zeta");
        builder.AddUsing("System.Text");
        builder.AddUsing("System");
        builder.AddUsing("Alpha");
        builder.AddUsing("System");
        var scope = builder.ScopeBrace("class Sample");
        var text = new string('x', 20000);
        builder.AppendLine(text);

        Assert.Equal("// generated\r\nusing System;\r\nusing System.Text;\r\nusing Alpha;\r\nusing Zeta;\r\n\r\nclass Sample\r\n{\r\n    " + text + "\r\n}\r\n", builder.Finalize());
        Assert.True(scope.IsDisposed);
        builder.AppendLine("next");
        Assert.Equal("next\r\n", builder.Finalize());
        Assert.Equal(string.Empty, builder.Finalize());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(4, 4)]
    [InlineData(20, 16)]
    public void IndentationKeepsExistingLimitsAndBlankLines(int requested, int spaces)
    {
        var builder = new ScopingStringBuilder(requested);
        for (var i = 0; i < ScopingStringBuilder.MaxIndent + 3; i++)
        {
            builder.IncrementIndent();
        }

        builder.AppendLine("x");
        builder.AppendLine();
        builder.AppendLine("y", false);
        Assert.Equal(new string(' ', ScopingStringBuilder.MaxIndent * spaces) + "x\r\n\r\ny\r\n", builder.Finalize());
    }

    [Fact]
    public void NamePrefixesDistinguishConstructedContainingTypesAndFormats()
    {
        var compilation = CSharpCompilation.Create("Prefixes", [CSharpSyntaxTree.ParseText("""
            namespace Alpha { public class Outer<T> { public class Inner<U> { public U Value; } } }
            namespace Beta { public class Outer<T> { public class Inner<U> { public U Value; } } }
            """, cancellationToken: TestContext.Current.CancellationToken)], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var body = new VisceralBody<TinyhandObject>((GeneratorExecutionContext?)null);
        foreach (var ns in new[] { "Alpha", "Beta" })
        {
            foreach (var (argument, name) in new[] { (SpecialType.System_Int32, "int"), (SpecialType.System_Int64, "long") })
            {
                var outer = compilation.GetTypeByMetadataName(ns + ".Outer`1")!.Construct(compilation.GetSpecialType(argument));
                var inner = outer.GetTypeMembers("Inner").Single().Construct(compilation.GetSpecialType(SpecialType.System_String));
                var member = inner.GetMembers("Value").Single();
                for (var repeat = 0; repeat < 2; repeat++)
                {
                    Assert.Equal($"{ns}.Outer<{name}>.Inner<string>.Value", body.SymbolToFullName(member));
                    Assert.Equal($"Outer<{name}>.Inner<string>.Value", body.SymbolToRegionalName(member));
                    Assert.Equal("Value", body.SymbolToLocalName(member));
                }
            }
        }
    }
}
