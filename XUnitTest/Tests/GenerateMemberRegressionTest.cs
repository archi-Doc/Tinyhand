// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand;
using Tinyhand.Generator;
using Xunit;

namespace XUnitTest.Tests;

public class GenerateMemberRegressionTest : IDisposable
{
    private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).Append(typeof(TinyhandSerializer).Assembly.Location).Distinct()
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x)).ToImmutableArray();

    private readonly string directory = Directory.CreateTempSubdirectory("Tinyhand.GeneratorTest.").FullName;

    [Theory]
    [InlineData("TG042", null)]
    [InlineData("TG043", "{")]
    public void InvalidFileProducesOneDiagnostic(string diagnosticId, string? text)
    {
        var result = this.Generate(text, false, out _);
        Assert.Equal(diagnosticId, Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void GeneratedNumbersCompileAndPreserveValuesAcrossCultures(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var result = this.Generate("Finite = 1.25 NaN = double.NaN Positive = double.PositiveInfinity Negative = double.NegativeInfinity Integer = -9223372036854775808", false, out var output);
            Assert.Empty(result.Diagnostics);
            this.CheckAssembly(output, type =>
            {
                Assert.Equal(1.25, type.GetProperty("Finite")!.GetValue(null));
                Assert.Equal(double.NaN, type.GetProperty("NaN")!.GetValue(null));
                Assert.Equal(double.PositiveInfinity, type.GetProperty("Positive")!.GetValue(null));
                Assert.Equal(double.NegativeInfinity, type.GetProperty("Negative")!.GetValue(null));
                Assert.Equal(long.MinValue, type.GetProperty("Integer")!.GetValue(null));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(4096)]
    public void EscapedGeneratedStringsPreserveEmbeddedNullsAndLength(int length)
    {
        var value = new string('x', length) + "\"\0tail\n\\";
        var text = "Text = \"" + new string('x', length) + "\\\"\\u0000tail\\n\\\\\"";
        var result = this.Generate(text, false, out var output);
        Assert.Empty(result.Diagnostics);
        this.CheckAssembly(output, type => Assert.Equal(value, type.GetProperty("Text")!.GetValue(null)));
    }

    [Fact]
    public void HashCommentsAreBoundedAndEscaped()
    {
        var value = new string('a', 200);
        var result = this.Generate($"Long = \"{value}\" Escaped = \"a&<b>\\n\\t\"", true, out var output);
        Assert.Empty(result.Diagnostics);
        var generated = string.Join("\n", result.GeneratedTrees.Select(x => x.ToString()));
        Assert.DoesNotContain(new string('a', 129), generated);
        Assert.Contains("a&amp;&lt;b&gt;\\n\\t", generated);
        this.CheckAssembly(output, type => Assert.Equal(HashedString.IdentifierToHash("Long"), type.GetProperty("Long")!.GetValue(null)));
    }

    public void Dispose() => Directory.Delete(this.directory, recursive: true);

    private GeneratorDriverRunResult Generate(string? text, bool hash, out Compilation output)
    {
        var path = Path.Combine(this.directory, "values.tinyhand");
        if (text is not null)
        {
            File.WriteAllText(path, text);
        }

        var source = $$"""
            using Tinyhand;
            [TinyhandGenerate{{(hash ? "Hash" : "Member")}}(@"{{path}}")]
            public static partial class GeneratedValues { }
            """;
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("GeneratedMembers", new[] { CSharpSyntaxTree.ParseText(source, options, cancellationToken: cancellationToken) }, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        return CSharpGeneratorDriver.Create(new[] { new TinyhandGeneratorV2().AsSourceGenerator(), new StaticRegistrationGenerator().AsSourceGenerator() }, parseOptions: options)
            .RunGeneratorsAndUpdateCompilation(compilation, out output, out _, cancellationToken).GetRunResult();
    }

    private void CheckAssembly(Compilation compilation, Action<Type> check)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(nameof(GenerateMemberRegressionTest), isCollectible: true);
        try
        {
            check(context.LoadFromStream(stream).GetType("GeneratedValues")!);
        }
        finally
        {
            context.Unload();
        }
    }
}
