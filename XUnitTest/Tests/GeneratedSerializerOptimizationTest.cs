// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand;
using Tinyhand.Generator;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class GeneratedSerializerOptimizationTest
{
    private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).Append(typeof(TinyhandSerializer).Assembly.Location).Distinct()
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x)).ToImmutableArray();

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(513)]
    [InlineData(65535)]
    [InlineData(65536)]
    public void ConstantKeysPreserveMessagePackBytes(int length)
        => this.CheckKey(new string('k', length), roundtrip: length <= 512);

    [Theory]
    [InlineData("quote\"slash\\line\n\t")]
    [InlineData("日本語のキー😀")]
    public void ConstantKeysSupportEscapedAndUnicodeNames(string key) => this.CheckKey(key, roundtrip: true);

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(130)]
    public void MissingKeysReconstructWithSkipLocalsInit(int count)
    {
        var fields = string.Join("\n", Enumerable.Range(0, count).Select(i => $"[Key(\"K{i}\")] public string V{i};"));
        var source = $$"""
            using Tinyhand;
            [module: System.Runtime.CompilerServices.SkipLocalsInit]
            [TinyhandObject]
            public partial class Model { {{fields}} }
            public static class Probe
            {
                public static string[] Decode(byte[] data)
                {
                    var value = TinyhandSerializer.DeserializeObject<Model>(data)!;
                    return [{{string.Join(", ", Enumerable.Range(0, count).Select(i => $"value.V{i}"))}}];
                }
            }
            """;
        WithAssembly(source, (type, generated) =>
        {
            // SkipLocalsInit makes explicit initialization essential for the stack-allocated fallback.
            Assert.Contains(count <= 64 ? "ulong deserializedFlag = 0;" : "deserializedFlag.Clear();", generated);
            var decode = type.GetMethod("Decode")!;
            var defaults = Enumerable.Repeat(string.Empty, count).ToArray();
            Assert.Equal(defaults, (string[])decode.Invoke(null, [new byte[] { 0x80 }])!);

            var writer = TinyhandWriter.CreateFromBytePool();
            try
            {
                writer.WriteMapHeader(4);
                writer.Write("K" + (count - 1));
                writer.Write("last");
                writer.Write("unknown");
                writer.WriteArrayHeader(1);
                writer.Write(42);
                writer.Write("K0");
                writer.Write("first");
                writer.Write("K0");
                writer.Write("duplicate");
                var data = writer.FlushAndGetArray();
                defaults[count - 1] = "last";
                defaults[0] = "duplicate";
                Assert.Equal(defaults, (string[])decode.Invoke(null, [data])!);
                // A previous invocation must not leave any presence bits set.
                Assert.Equal(Enumerable.Repeat(string.Empty, count), (string[])decode.Invoke(null, [new byte[] { 0x80 }])!);
            }
            finally
            {
                writer.Dispose();
            }
        });
    }

    [Fact]
    public void CollectionCodersCompileForNullableStructsAndJaggedArrays()
    {
        WithAssembly("""
            using Tinyhand;
            using System.Collections.Generic;
            [TinyhandObject]
            public partial struct ModelPoint { [Key(0)] public int Number; }
            [TinyhandObject(SkipDefaultValues = false)]
            public partial class Model
            {
                [Key(0)] public ModelPoint[] Empty = [];
                [Key(1)] public List<ModelPoint?> Points = [new ModelPoint { Number = 7 }, null];
                [Key(2)] public ModelPoint?[][] Nested = [[], [new ModelPoint { Number = 11 }, null]];
            }
            public static class Probe
            {
                public static bool Run()
                {
                    var value = TinyhandSerializer.DeserializeObject<Model>(TinyhandSerializer.SerializeObject(new Model()))!;
                    return object.ReferenceEquals(value.Empty, System.Array.Empty<ModelPoint>())
                        && value.Points.Count == 2 && value.Points[0]?.Number == 7 && value.Points[1] is null
                        && object.ReferenceEquals(value.Nested[0], System.Array.Empty<ModelPoint?>())
                        && value.Nested[1][0]?.Number == 11 && value.Nested[1][1] is null;
                }
            }
            """, (type, _) => Assert.Equal(true, type.GetMethod("Run")!.Invoke(null, null)));
    }

    private static void WithAssembly(string source, Action<Type, string> check)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        source = source.Replace("Model", "Model_" + Guid.NewGuid().ToString("N"), StringComparison.Ordinal);
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("SerializerOptimization_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken)], References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release, nullableContextOptions: NullableContextOptions.Enable));
        var result = CSharpGeneratorDriver.Create(
            [new TinyhandGeneratorV2().AsSourceGenerator(), new StaticRegistrationGenerator().AsSourceGenerator()], parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, cancellationToken).GetRunResult();
        Assert.DoesNotContain(result.Diagnostics, x => x.Severity == DiagnosticSeverity.Error || x.Id == "CS8785");
        using var stream = new MemoryStream();
        var emit = output.Emit(stream, cancellationToken: cancellationToken);
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(nameof(GeneratedSerializerOptimizationTest), isCollectible: true);
        try
        {
            check(context.LoadFromStream(stream).GetType("Probe")!, string.Join("\n", result.GeneratedTrees.Select(x => x.ToString())));
        }
        finally
        {
            context.Unload();
        }
    }

    private void CheckKey(string key, bool roundtrip)
    {
        var literal = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(key, quote: true);
        var source = $$"""
            using Tinyhand;
            [TinyhandObject]
            public partial class Model { [Key({{literal}})] public int Value; }
            public static class Probe
            {
                public static byte[] Encode() => TinyhandSerializer.SerializeObject(new Model { Value = 123 });
                public static int Decode(byte[] data) => TinyhandSerializer.DeserializeObject<Model>(data)!.Value;
            }
            """;
        WithAssembly(source, (type, _) =>
        {
            var bytes = (byte[])type.GetMethod("Encode")!.Invoke(null, null)!;
            var writer = TinyhandWriter.CreateFromBytePool();
            try
            {
                writer.WriteMapHeader(1);
                writer.Write(key);
                writer.Write(123);
                Assert.Equal(writer.FlushAndGetArray(), bytes);
                if (roundtrip)
                {
                    Assert.Equal(123, type.GetMethod("Decode")!.Invoke(null, [bytes]));
                }
            }
            finally
            {
                writer.Dispose();
            }
        });
    }
}
