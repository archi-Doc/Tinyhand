// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand.Generator;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class GeneratedDuplicateKeySecurityTest
{
    private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).Append(typeof(TinyhandSerializer).Assembly.Location).Distinct()
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x)).ToImmutableArray();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KnownDuplicatesAreRejectedBeforeSecondAssignment(bool firstNil)
    {
        CheckDuplicate<DuplicateMapModel>(firstNil);
        CheckDuplicate<DuplicateAlternateModel>(firstNil);
    }

    [Theory]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(1025)]
    public void LargeSchemasInitializeAndBoundDuplicateTrackingStorage(int count)
    {
        var fields = string.Join("\n", Enumerable.Range(0, count).Select(i => $"[Key(\"K{i}\")] public int? V{i};"));
        var source = $$"""
            using Tinyhand;
            using Tinyhand.IO;
            [module: System.Runtime.CompilerServices.SkipLocalsInit]
            [TinyhandObject]
            public partial class Model { {{fields}} }
            public static class Probe
            {
                public static int? Decode(byte[] data, bool untrusted)
                {
                    var options = TinyhandSerializerOptions.Standard with
                    {
                        Security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData,
                    };
                    var reader = new TinyhandReader(data);
                    Model? value = null;
                    TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
                    return value!.V{{count - 1}};
                }
            }
            """;
        WithAssembly(source, (probe, generated) =>
        {
            if (count <= 64)
            {
                Assert.Contains("ulong seenKeys = 0;", generated);
            }
            else
            {
                Assert.Contains(count <= 1024 ? "? stackalloc ulong[2] : default;" : "? new ulong[17] : default;", generated);
                Assert.Contains("seenKeys.Clear();", generated);
            }

            var decode = probe.GetMethod("Decode")!;
            var last = "K" + (count - 1);
            var valid = CreateMap(("K0", 11), (last, 22));
            var duplicate = CreateMap((last, 11), (last, 22));
            Assert.Equal(22, decode.Invoke(null, [valid, true]));
            var exception = Assert.Throws<TargetInvocationException>(() => decode.Invoke(null, [duplicate, true]));
            Assert.IsType<TinyhandException>(exception.InnerException);
            Assert.Equal(22, decode.Invoke(null, [duplicate, false]));
            Assert.Equal(22, decode.Invoke(null, [valid, true]));
        });
    }

    [Fact]
    public void LargeReconstructionBitmapUsesHeapStorageAfterValidation()
    {
        var fields = string.Join("\n", Enumerable.Range(0, 1025).Select(i => $"[Key(\"K{i}\")] public int V{i} = 42;"));
        var source = $$"""
            using Tinyhand;
            using Tinyhand.IO;
            [module: System.Runtime.CompilerServices.SkipLocalsInit]
            [TinyhandObject]
            public partial class Model { {{fields}} }
            public static class Probe
            {
                public static int[] Decode(byte[] data)
                {
                    var reader = new TinyhandReader(data);
                    Model? value = new Model { V0 = 7, V1024 = 7 };
                    TinyhandSerializer.DeserializeObject(ref reader, ref value, TinyhandSerializerOptions.Standard);
                    return [value!.V0, value.V1024];
                }
            }
            """;
        WithAssembly(source, (probe, generated) =>
        {
            Assert.Contains("Span<ulong> deserializedFlag = new ulong[17];", generated);
            Assert.Contains("deserializedFlag.Clear();", generated);
            Assert.True(generated.IndexOf("reader.ReadMapHeaderOrEmptyArray()", StringComparison.Ordinal)
                < generated.IndexOf("Span<ulong> deserializedFlag = new ulong[17];", StringComparison.Ordinal));
            var decode = probe.GetMethod("Decode")!;
            Assert.Equal(new[] { 42, 42 }, (int[])decode.Invoke(null, [new byte[] { 0x80 }])!);
            Assert.Equal(new[] { 42, 123 }, (int[])decode.Invoke(null, [CreateMap(("K1024", 123))])!);
            Assert.Equal(new[] { 42, 42 }, (int[])decode.Invoke(null, [new byte[] { 0x80 }])!);
        });
    }

    private static void CheckDuplicate<T>(bool firstNil)
        where T : class, ITinyhandSerializable<T>, IDuplicateAssignmentProbe, new()
    {
        var data = CreateMap(("Value", firstNil ? null : 1), ("Value", 2));
        var options = TinyhandSerializerOptions.ConvertToString with { Security = TinyhandSecurity.UntrustedData };
        T? value = new T();
        Assert.Throws<TinyhandException>(() =>
        {
            var reader = new TinyhandReader(data);
            try
            {
                TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
            }
            finally
            {
                Assert.Equal(0, reader.Depth);
            }
        });
        Assert.Equal(firstNil ? 0 : 1, value!.Value);
        Assert.Equal(firstNil ? 0 : 1, value.Assignments);

        foreach (var security in new[] { TinyhandSecurity.TrustedData, TinyhandSecurity.UntrustedData.WithRejectDuplicateKeys(false) })
        {
            value = new T();
            var reader = new TinyhandReader(data);
            TinyhandSerializer.DeserializeObject(ref reader, ref value, options with { Security = security });
            Assert.Equal(2, value!.Value);
            Assert.Equal(firstNil ? 1 : 2, value.Assignments);
            Assert.True(reader.End);
        }

        // Repeated unknown fields have no member to overwrite and retain forward-compatible skipping.
        value = new T();
        var unknownReader = new TinyhandReader(CreateMap(("unknown", 1), ("unknown", 2), ("Value", 3)));
        TinyhandSerializer.DeserializeObject(ref unknownReader, ref value, options);
        Assert.Equal(3, value!.Value);
        Assert.Equal(1, value.Assignments);
        Assert.True(unknownReader.End);
    }

    private static byte[] CreateMap(params (string Key, int? Value)[] entries)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteMapHeader(entries.Length);
            foreach (var entry in entries)
            {
                writer.Write(entry.Key);
                if (entry.Value.HasValue)
                {
                    writer.Write(entry.Value.Value);
                }
                else
                {
                    writer.WriteNil();
                }
            }

            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static void WithAssembly(string source, Action<Type, string> check)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        source = System.Text.RegularExpressions.Regex.Replace(source, @"\bModel\b", "Model_" + Guid.NewGuid().ToString("N"));
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("DuplicateKeys_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken)], References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release, nullableContextOptions: NullableContextOptions.Enable));
        var result = CSharpGeneratorDriver.Create(
            [new TinyhandGenerator().AsSourceGenerator(), new StaticRegistrationGenerator().AsSourceGenerator()], parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, cancellationToken).GetRunResult();
        Assert.DoesNotContain(result.Diagnostics, x => x.Severity == DiagnosticSeverity.Error || x.Id == "CS8785");
        using var stream = new MemoryStream();
        var emit = output.Emit(stream, cancellationToken: cancellationToken);
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(nameof(GeneratedDuplicateKeySecurityTest), isCollectible: true);
        try
        {
            check(context.LoadFromStream(stream).GetType("Probe")!, string.Join("\n", result.GeneratedTrees.Select(x => x.ToString())));
        }
        finally
        {
            context.Unload();
        }
    }
}

public interface IDuplicateAssignmentProbe
{
    int Value { get; set; }

    int Assignments { get; }
}

[TinyhandObject]
public partial class DuplicateMapModel : IDuplicateAssignmentProbe
{
    private int fieldValue;

    [Key("Value")]
    public int Value
    {
        get => this.fieldValue;
        set
        {
            this.Assignments++;
            this.fieldValue = value;
        }
    }

    [IgnoreMember]
    public int Assignments { get; private set; }
}

[TinyhandObject(AddAlternateKey = true)]
public partial class DuplicateAlternateModel : IDuplicateAssignmentProbe
{
    private int fieldValue;

    [Key(0)]
    public int Value
    {
        get => this.fieldValue;
        set
        {
            this.Assignments++;
            this.fieldValue = value;
        }
    }

    [IgnoreMember]
    public int Assignments { get; private set; }
}
