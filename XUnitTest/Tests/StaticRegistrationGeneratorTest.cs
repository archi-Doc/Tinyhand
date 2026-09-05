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
    public void AnonymousCollectionsAreNotRegistered()
    {
        var result = Generate("""
            using System.Collections.Generic;
            public static class Consumer
            {
                public static object Run()
                {
                    var values = new[] { new { Number = 1 } };
                    return values;
                }
                public static List<int> Known() => new();
            }
            """, out var output);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
        var generated = string.Join("\n", result.GeneratedTrees.Select(x => x.ToString()));
        Assert.Contains("RegisterListFormatter<int>()", generated);
        Assert.DoesNotContain("RegisterArray<", generated);
    }

    [Fact]
    public void ExtensionDeclarationsAreNotRegistered()
    {
        var result = Generate("""
            using System.Collections.Generic;
            public enum Result
            {
                Success,
            }
            public static class ResultExtensions
            {
                extension(Result result)
                {
                    public bool IsSuccess => result == Result.Success;
                }
            }
            public static class Consumer
            {
                public static List<int> Known() => new();
            }
            """, out var output);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
        var generated = string.Join("\n", result.GeneratedTrees.Select(x => x.ToString()));
        Assert.Contains("RegisterListFormatter<int>()", generated);
    }

    [Fact]
    public void StringConvertibleRegistrationRequiresMatchingSelfType()
    {
        var result = Generate("""
            using System;
            using Tinyhand;
            [TinyhandObject]
            public partial class Base : Arc.IStringConvertible<Base>
            {
                public static int MaxStringLength => 1;
                public int GetStringLength() => 1;
                public bool TryFormat(Span<char> destination, out int written, Arc.IConversionOptions? conversionOptions = null)
                {
                    written = 0;
                    return true;
                }
                public static bool TryParse(ReadOnlySpan<char> source, out Base? instance, out int read, Arc.IConversionOptions? conversionOptions = null)
                {
                    instance = new();
                    read = source.Length;
                    return true;
                }
            }
            [TinyhandObject]
            public partial class Derived : Base
            {
            }
            """);
        Assert.Empty(result.Diagnostics);
        var generated = string.Join("\n", result.GeneratedTrees.Select(x => x.ToString()));
        Assert.Contains("RegisterStringConvertible<global::Base>()", generated);
        Assert.DoesNotContain("RegisterStringConvertible<global::Derived>()", generated);
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("Missing<int>")]
    [InlineData("Owner<int>.GoshujinClass")]
    [InlineData("Missing[]")]
    public void UnresolvedCollectionElementsAreNotRegistered(string element)
    {
        var result = Generate($$"""
            using System.Collections.Generic;
            public partial class Owner<T> { }
            public static class Consumer
            {
                public static List<{{element}}> Pending() => new();
                public static List<int> Known() => new();
            }
            """);
        Assert.Empty(result.Diagnostics);
        var generated = string.Join("\n", result.GeneratedTrees.Select(x => x.ToString()));
        Assert.Contains("RegisterListFormatter<int>()", generated);
        Assert.DoesNotContain("Missing", generated);
        Assert.DoesNotContain("GoshujinClass", generated);
    }

    [Fact]
    public void UnresolvedNestedTypesInGenericHelpersDoNotCrashSubstitution()
    {
        var result = Generate("""
            using System.Collections.Generic;
            public partial class Owner<T> { }
            public static class Consumer
            {
                public static void Run() => Helper<int>();
                public static void Helper<T>()
                {
                    var owner = new Owner<T>.GoshujinClass();
                    var owners = new List<Owner<T>.GoshujinClass>();
                    var known = new List<T>();
                }
            }
            """);
        Assert.Empty(result.Diagnostics);
        var generated = string.Join("\n", result.GeneratedTrees.Select(x => x.ToString()));
        Assert.Contains("RegisterListFormatter<int>()", generated);
        Assert.DoesNotContain("GoshujinClass", generated);
    }

    [Fact]
    public void AnonymousTypesInGenericHelpersDoNotCrashSubstitution()
    {
        var result = Generate("""
            using System.Collections.Generic;
            public static class Consumer
            {
                public static object Run() => Helper<int>();
                public static object Helper<T>()
                {
                    var value = new { Item = default(T) };
                    var values = new[] { value };
                    var known = new List<T>();
                    return values;
                }
            }
            """, out var output);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
        var generated = string.Join("\n", result.GeneratedTrees.Select(x => x.ToString()));
        Assert.Contains("RegisterListFormatter<int>()", generated);
        Assert.DoesNotContain("RegisterArray<", generated);
    }

    [Fact]
    public void AnotherGeneratorCanSupplyUnresolvedNestedOwner()
    {
        var result = Generate("""
            using System.Collections.Generic;
            public partial class Owner<T> { }
            public static class Consumer
            {
                public static object Run() => Helper<int>();
                public static object Helper<T>()
                {
                    var owner = new Owner<T>.GoshujinClass();
                    var known = new List<T>();
                    return owner;
                }
            }
            """, out var output, new OwnerDeclarationGenerator().AsSourceGenerator());
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
        var registration = result.GeneratedTrees.Single(x => x.FilePath.EndsWith("Tinyhand.StaticRegistration.g.cs", StringComparison.Ordinal)).ToString();
        Assert.Contains("RegisterListFormatter<int>()", registration);
        Assert.DoesNotContain("GoshujinClass", registration);
    }

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

    private static GeneratorDriverRunResult Generate(string source, out Compilation output, params ISourceGenerator[] additionalGenerators)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("GeneratorTests", new[] { CSharpSyntaxTree.ParseText(source, parseOptions) }, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        return CSharpGeneratorDriver.Create(new[] { new StaticRegistrationGenerator().AsSourceGenerator() }.Concat(additionalGenerators), parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out output, out _).GetRunResult();
    }

    private sealed class OwnerDeclarationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, static (ctx, _) =>
                ctx.AddSource("Owner.g.cs", "public partial class Owner<T> { public sealed class GoshujinClass { } }"));
        }
    }
}
