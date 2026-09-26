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

namespace XUnitTest.Tests;

public class GeneratorConcurrencyTest
{
    private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).Append(typeof(TinyhandSerializer).Assembly.Location).Distinct()
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x)).ToImmutableArray();

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    [Fact]
    public async Task OneGeneratorInstanceServesConcurrentCompilations()
    {
        // Roslyn shares a generator instance among compilations (e.g. projects or target frameworks that use the same analyzer)
        // and may run them at the same time, so the output of a run must not depend on the other runs.
        var generator = new TinyhandGenerator();
        var compilations = Enumerable.Range(0, 4).Select(CreateCompilation).ToArray();
        var expected = compilations.Select(x => Generate(generator, x)).ToArray();
        Assert.Equal(compilations.Length, expected.Distinct(StringComparer.Ordinal).Count()); // A mix-up changes the output.

        var cancellationToken = TestContext.Current.CancellationToken;
        await Task.WhenAll(Enumerable.Range(0, 64).Select(n => Task.Run(
            () => Assert.Equal(expected[n % compilations.Length], Generate(generator, compilations[n % compilations.Length])),
            cancellationToken)));
    }

    private static Compilation CreateCompilation(int index)
    {
        // Every other compilation has a custom namespace, and the assembly name appears in the generated loader.
        var option = index % 2 == 0 ? $"[TinyhandGeneratorOption(CustomNamespace = \"Custom{index}\")]" : string.Empty;
        var source = $$"""
            using Tinyhand;

            namespace Space{{index}};

            {{option}}
            [TinyhandObject]
            public partial class Model{{index}}
            {
                [Key(0)]
                public int Value{{index}} { get; set; }
            }
            """;

        return CSharpCompilation.Create($"GeneratorConcurrency{index}", [CSharpSyntaxTree.ParseText(source, ParseOptions, path: $"Model{index}.cs")], References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
    }

    private static string Generate(TinyhandGenerator generator, Compilation compilation)
    {
        var result = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: ParseOptions).RunGenerators(compilation).GetRunResult();
        return string.Join("\n", result.Diagnostics.Select(x => x.ToString())
            .Concat(result.GeneratedTrees.OrderBy(x => x.FilePath, StringComparer.Ordinal).Select(x => x.FilePath + "\n" + x.GetText())));
    }
}
