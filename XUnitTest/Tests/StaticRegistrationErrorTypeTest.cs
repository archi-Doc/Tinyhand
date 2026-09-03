// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand.Generator;
using Xunit;

namespace XUnitTest;

/// <summary>Tests registration when another generator supplies missing types.</summary>
public class StaticRegistrationErrorTypeTest
{
    [Theory]
    [InlineData("public class C { public Missing? Value; public (Missing, int) Pair; public Missing[] Items; }")]
    [InlineData("public class C { public object M() { var x = new { Id = 1 }; return (x, new[] { x }); } }")]
    [InlineData("public class Generic<T> { public static void M() { Missing<T>.Nested x = default!; } } public class C { public void M() => Generic<int>.M(); }")]
    public void UnresolvedAndAnonymousTypesDoNotProduceInvalidRegistrations(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x));
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("UnresolvedRegistration",
            [CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current.CancellationToken)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var result = CSharpGeneratorDriver.Create([new StaticRegistrationGenerator().AsSourceGenerator()], parseOptions: options)
            .RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        Assert.Empty(result.Diagnostics);
        Assert.All(result.GeneratedTrees, tree => Assert.DoesNotContain(tree.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain("Missing", string.Join("\n", result.GeneratedTrees.Select(x => x.ToString())));
        Assert.DoesNotContain("anonymous type", string.Join("\n", result.GeneratedTrees.Select(x => x.ToString())));
    }
}
