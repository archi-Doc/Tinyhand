// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand;
using Tinyhand.Generator;
using Xunit;

namespace XUnitTest;

public class StaticRegistrationGeneratorTest
{
    private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).Append(typeof(TinyhandSerializer).Assembly.Location).Distinct()
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x)).ToImmutableArray();

    [Fact]
    public void RejectsOpenRegistrationRoot()
    {
        var result = Generate("""
            [assembly: Tinyhand.TinyhandRegister(typeof(System.Collections.Generic.List<>))]
            """);
        Assert.Contains(result.Diagnostics, x => x.Id == "THAOT003");
        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "THAOT999");
    }

    [Theory]
    [InlineData("List<T>")]
    [InlineData("KeyValuePair<T, T>")]
    public void StopsExpandingGenericGraph(string argument)
    {
        var result = Generate($$"""
            using Tinyhand;
            using System.Collections.Generic;
            [assembly: TinyhandRegister(typeof(Grow<int>))]
            [TinyhandObject]
            public partial class Grow<T>
            {
                [Key(0)] public Grow<{{argument}}>? Next { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, x => x.Id == "THAOT002");
        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "THAOT999");
    }

    [Fact]
    public void IgnoredPropertyDoesNotExpandThroughItsBackingField()
    {
        var result = Generate("""
            using Tinyhand;
            using System.Collections.Generic;
            [assembly: TinyhandRegister(typeof(Grow<int>))]
            [TinyhandObject]
            public partial class Grow<T>
            {
                [IgnoreMember] public Grow<List<T>>? Next { get; set; }
            }
            """);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ReportsInaccessibleMemberDependency()
    {
        var result = Generate("""
            using Tinyhand;
            public class Outer
            {
                private enum Hidden { One }
                [TinyhandObject]
                public partial class Model
                {
                    [Key(0)] private Hidden value;
                }
                public static byte[] Run() => TinyhandSerializer.Serialize(new Model());
            }
            """);
        Assert.Contains(result.Diagnostics, x => x.Id == "THAOT001");
        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "THAOT999");
    }

    [Fact]
    public void PartialScopeMakesPrivateEnumAccessible()
    {
        var result = Generate("""
            using Tinyhand;
            public static partial class Outer
            {
                private enum Hidden { One }
                public static byte[] Run() => TinyhandSerializer.Serialize(Hidden.One);
            }
            """, out var output);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task ParallelCompilationsDoNotShareModelCaches()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            // Identical full names describe reference types in one compilation
            // and value types in another; their coders must never be shared.
            var source = $$"""
                using Tinyhand;
                namespace Shared;
                [TinyhandObject]
                public partial {{(i % 2 == 0 ? "class" : "struct")}} Model
                {
                    [Key(0)] public int Number { get; set; }
                }
                [TinyhandObject]
                public partial class Container
                {
                    [Key(0)] public Model Value { get; set; } = new();
                }
                """;
            var options = new CSharpParseOptions(LanguageVersion.Preview);
            var compilation = CSharpCompilation.Create("ParallelGenerators", new[] { CSharpSyntaxTree.ParseText(source, options, cancellationToken: cancellationToken) }, References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
            var result = CSharpGeneratorDriver.Create(new[] { new TinyhandGeneratorV2().AsSourceGenerator(), new StaticRegistrationGenerator().AsSourceGenerator() }, parseOptions: options)
                .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, cancellationToken).GetRunResult();
            Assert.DoesNotContain(result.Diagnostics, x => x.Severity == DiagnosticSeverity.Error || x.Id == "CS8785");
            Assert.DoesNotContain(output.GetDiagnostics(cancellationToken), x => x.Severity == DiagnosticSeverity.Error);
        }, cancellationToken)));
    }

    private static GeneratorDriverRunResult Generate(string source) => Generate(source, out _);

    private static GeneratorDriverRunResult Generate(string source, out Compilation output)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("GeneratorTests", new[] { CSharpSyntaxTree.ParseText(source, parseOptions) }, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        return CSharpGeneratorDriver.Create(new[] { new StaticRegistrationGenerator().AsSourceGenerator() }, parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out output, out _).GetRunResult();
    }
}
