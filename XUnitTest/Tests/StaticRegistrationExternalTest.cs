// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand.Generator;
using Xunit;

namespace XUnitTest;

public partial class StaticRegistrationGeneratorTest
{
    [Theory]
    [InlineData("public partial class Model { }")]
    [InlineData("public partial class Model { }", ", AddImmutable = true")]
    [InlineData("public partial struct Model { }")]
    [InlineData("public partial class Model { [TinyhandObject(External = true)] public partial class Nested { } }")]
    [InlineData("[TinyhandUnion(0, typeof(Derived))] public partial interface Model { } [TinyhandObject] public partial class Derived : Model { }")]
    public void ExternalDeclarationsDoNotRequireAnImplementation(string declaration, string settings = "")
    {
        var result = Generate($"using Tinyhand; [TinyhandObject(External = true{settings})] " + declaration,
            out var output, new TinyhandGeneratorV2().AsSourceGenerator());
        AssertSuccessfulCompilation(result, output);
        var registration = RegistrationSource(result);
        Assert.DoesNotContain("RegisterObject<global::Model", registration);
        Assert.DoesNotContain("Immutable", registration);
    }

    [Fact]
    public void NormalObjectsUnionsAndImmutableTypesStillRegister()
    {
        var result = Generate("""
            using Tinyhand;
            [TinyhandObject(External = false)] public partial class Model { }
            [TinyhandObject(AddImmutable = true)] public partial class ImmutableModel { [Key(0)] public int Number { get; set; } }
            [TinyhandObject] public partial struct Value { }
            [TinyhandUnion(0, typeof(ModelUnion))] public partial interface IUnion { }
            [TinyhandObject] public partial class ModelUnion : IUnion { }
            """, out var output, new TinyhandGeneratorV2().AsSourceGenerator());
        AssertSuccessfulCompilation(result, output);
        var registration = RegistrationSource(result);
        foreach (var name in new[] { "Model", "Value", "IUnion", "ModelUnion", "ImmutableModel.Immutable" })
        {
            Assert.Contains($"RegisterObject<global::{name}>()", registration);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(", ITinyhandReconstructable<Model>")]
    [InlineData(", ITinyhandCloneable<Model>")]
    public void IncompleteInterfacesDoNotRegister(string interfaces)
    {
        var source = ManualModelSource("", interfaces);
        var result = Generate(source, out var output);
        AssertSuccessfulCompilation(result, output);
        Assert.DoesNotContain("RegisterObject<global::Model>()", RegistrationSource(result));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[TinyhandObject(External = true, AddImmutable = true)]")]
    public void CompleteSelfTypeInterfacesRegisterButInheritedOtherTypeDoesNot(string attribute)
    {
        var result = Generate(ManualModelSource(attribute) + " public class Derived : Model { }", out var output);
        AssertSuccessfulCompilation(result, output);
        var registration = RegistrationSource(result);
        Assert.Contains("RegisterObject<global::Model>()", registration);
        Assert.DoesNotContain("RegisterObject<global::Derived>()", registration);
        Assert.DoesNotContain("Immutable", registration);
    }

    [Fact]
    public void ExternalDependenciesAreExploredAfterClosingGenericType()
    {
        var result = Generate("""
            using Tinyhand;
            using System.Collections.Generic;
            [TinyhandObject(External = true)]
            public partial class Model<T>
            {
                public List<T> Values { get; set; } = new();
                public Dictionary<string, T[]> Map { get; set; } = new();
            }
            public class Consumer { public Model<int> Value = new(); }
            """, out var output, new TinyhandGeneratorV2().AsSourceGenerator());
        AssertSuccessfulCompilation(result, output);
        var registration = RegistrationSource(result);
        Assert.DoesNotContain("RegisterObject<global::Model<int>>()", registration);
        Assert.Contains("RegisterListFormatter<int>()", registration);
        Assert.Contains("RegisterDictionaryFormatter<string, int[]>()", registration);
        Assert.Contains("RegisterArray<int>()", registration);
    }

    [Fact]
    public void ExplicitExternalRootReportsDelegationAtTheAttribute()
    {
        var result = Generate("""
            using Tinyhand;
            [assembly: TinyhandRegister(typeof(Model<int>))]
            [TinyhandObject(External = true)] public partial class Model<T> { public T[] Values = []; }
            """, out var output, new TinyhandGeneratorV2().AsSourceGenerator());
        AssertSuccessfulCompilation(result, output);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("THAOT004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal(1, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.DoesNotContain("RegisterObject<global::Model<int>>()", RegistrationSource(result));
        Assert.Contains("RegisterArray<int>()", RegistrationSource(result));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(true, true)]
    public void ReferencedTypeUsesActualInterfaces(bool implemented, bool immutable = false)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var source = implemented ? ManualModelSource("[TinyhandObject(External = true)]") :
            "[Tinyhand.TinyhandObject(External = true)] public class Model { }";
        if (immutable)
        {
            source = "[Tinyhand.TinyhandObject(AddImmutable = true)] public partial class Model { [Tinyhand.Key(0)] public int Number { get; set; } }";
        }

        Compilation library = CSharpCompilation.Create("ExternalModels", new[] { CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current.CancellationToken) }, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        if (immutable)
        {
            var generated = CSharpGeneratorDriver.Create(new[] { new TinyhandGeneratorV2().AsSourceGenerator(), new StaticRegistrationGenerator().AsSourceGenerator() }, parseOptions: options)
                .RunGeneratorsAndUpdateCompilation(library, out library, out _, TestContext.Current.CancellationToken).GetRunResult();
            AssertSuccessfulCompilation(generated, library);
        }
        using var stream = new MemoryStream();
        var emitted = library.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        var compilation = CSharpCompilation.Create("ExternalConsumer",
            new[] { CSharpSyntaxTree.ParseText("[assembly: Tinyhand.TinyhandRegister(typeof(Model))]", options, cancellationToken: TestContext.Current.CancellationToken) },
            References.Add(MetadataReference.CreateFromImage(stream.ToArray())), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var result = CSharpGeneratorDriver.Create(new[] { new StaticRegistrationGenerator().AsSourceGenerator() }, parseOptions: options)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, TestContext.Current.CancellationToken).GetRunResult();
        AssertSuccessfulCompilation(result, output);
        Assert.Equal(implemented, RegistrationSource(result).Contains("RegisterObject<global::Model>()", StringComparison.Ordinal));
        Assert.Equal(!implemented, result.Diagnostics.Any(x => x.Id == "THAOT004"));
        if (immutable)
        {
            Assert.Contains("RegisterObject<global::Model.Immutable>()", RegistrationSource(result));
        }
    }

    private static string ManualModelSource(string attribute, string interfaces = ", ITinyhandReconstructable<Model>, ITinyhandCloneable<Model>") => $$"""
        using Tinyhand;
        using Tinyhand.IO;
        {{attribute}}
        public partial class Model : ITinyhandSerializable<Model>{{interfaces}}
        {
            public static void Serialize(ref TinyhandWriter writer, scoped ref Model? value, TinyhandSerializerOptions options) { }
            public static void Deserialize(ref TinyhandReader reader, scoped ref Model? value, TinyhandSerializerOptions options) { }
            public static void Reconstruct(scoped ref Model? value, TinyhandSerializerOptions options) => value = new();
            public static Model? Clone(scoped ref Model? value, TinyhandSerializerOptions options) => new();
        }
        """;

    private static string RegistrationSource(GeneratorDriverRunResult result) => result.GeneratedTrees
        .Single(x => x.FilePath.EndsWith("Tinyhand.StaticRegistration.g.cs", StringComparison.Ordinal)).ToString();

    private static void AssertSuccessfulCompilation(GeneratorDriverRunResult result, Compilation output)
    {
        Assert.DoesNotContain(result.Diagnostics, x => x.Severity == DiagnosticSeverity.Error || x.Id == "CS8785");
        Assert.DoesNotContain(output.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error);
    }
}
