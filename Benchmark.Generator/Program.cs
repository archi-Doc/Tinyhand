// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tinyhand;

// Each measurement creates a fresh driver. Cold runs also create a fresh compilation;
// warm runs reuse the compilation and its cached symbols. Input parsing and output validation are not timed.
var iterations = int.Parse(Option("--iterations", "15"));
if (iterations < 1)
{
    throw new ArgumentOutOfRangeException(nameof(iterations));
}

// An isolated context is necessary: the default context would resolve a baseline with the
// same assembly identity to the current DLL listed in this application's dependencies.
var loadContext = new AssemblyLoadContext("GeneratorBenchmark");
var generatorAssembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(Option("--generator", Path.Combine(AppContext.BaseDirectory, "TinyhandGenerator.dll"))));
Console.WriteLine($"Generator: {generatorAssembly.Location}");
var generatorNames = args.Contains("--main-only") ? new[] { "TinyhandGenerator" } : new[] { "TinyhandGenerator", "StaticRegistrationGenerator" };
var generators = generatorNames.Select(name => ((IIncrementalGenerator)Activator.CreateInstance(generatorAssembly.GetType("Tinyhand.Generator." + name, throwOnError: true)!)!).AsSourceGenerator()).ToArray();
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Append(typeof(TinyhandSerializer).Assembly.Location).Distinct(StringComparer.OrdinalIgnoreCase)
    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();
var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
var results = new List<Measurement>();
foreach (var scenario in new[] { "Models", "Collections", "GenericNoise" })
{
    var trees = CreateSources(scenario).Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, path: $"Input{index}.cs")).ToArray();
    Compilation CreateCompilation() => CSharpCompilation.Create("GeneratorPerformance", trees, references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
    GeneratorDriver Run(Compilation compilation) => CSharpGeneratorDriver.Create(generators, parseOptions: parseOptions).RunGenerators(compilation);

    var warmCompilation = CreateCompilation();
    var expected = Run(warmCompilation).GetRunResult();
    if (expected.Diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error) || expected.Results.Any(x => x.Exception is not null))
    {
        throw new InvalidOperationException(string.Join(Environment.NewLine, expected.Diagnostics));
    }

    if (!args.Contains("--main-only"))
    {
        var errors = warmCompilation.AddSyntaxTrees(expected.GeneratedTrees).GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(x => x.ToString())));
        }
    }

    var expectedText = Output(expected);
    var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(expectedText)));
    for (var i = 0; i < 5; i++)
    {
        Run(CreateCompilation());
    }

    foreach (var mode in new[] { "Cold", "Warm" })
    {
        var times = new double[iterations];
        var allocations = new long[iterations];
        for (var i = 0; i < iterations; i++)
        {
            var compilation = mode == "Cold" ? CreateCompilation() : warmCompilation;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            var start = Stopwatch.GetTimestamp();
            var driver = Run(compilation);
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            allocations[i] = GC.GetTotalAllocatedBytes(precise: true) - before;
            if (Output(driver.GetRunResult()) != expectedText)
            {
                throw new InvalidOperationException("Non-deterministic generator output.");
            }
        }

        Array.Sort(times);
        Array.Sort(allocations);
        var result = new Measurement(scenario, mode, times[iterations / 2], allocations[iterations / 2], expectedText.Length, fingerprint);
        results.Add(result);
        Console.WriteLine($"{scenario,-14} {mode,-4}: {result.MedianMilliseconds,9:F2} ms {result.MedianAllocatedBytes / 1024.0 / 1024,8:F2} MiB; output {fingerprint[..12]}");
    }
}

File.WriteAllText(Option("--output", "generator-performance.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));

string Option(string name, string fallback)
{
    var index = Array.IndexOf(args, name);
    return index < 0 ? fallback : args[index + 1];
}

static string Output(GeneratorDriverRunResult result) => string.Join("\n", result.Diagnostics.Select(x => x.ToString())
    .Concat(result.GeneratedTrees.OrderBy(x => x.FilePath, StringComparer.Ordinal).Select(x => x.FilePath + "\n" + x.GetText())));

static IEnumerable<string> CreateSources(string scenario)
{
    for (var file = 0; file < 10; file++)
    {
        var source = new StringBuilder("using System; using System.Collections.Generic; using Tinyhand;\n");
        source.AppendLine($"namespace Performance{file};");
        if (scenario == "GenericNoise")
        {
            source.AppendLine("[TinyhandObject] public partial class Box<T> { [Key(0)] public T Value { get; set; } = default!; }");
            source.AppendLine("public static class Consumer {");
            for (var method = 0; method < 100; method++)
            {
                source.AppendLine($"public static Dictionary<string, List<(int, string)>> Method{method}(List<Tuple<int, string>> input) => new Dictionary<string, List<(int, string)>>();");
            }

            source.AppendLine("public static Box<int> Create() => new Box<int>(); }");
        }
        else
        {
            source.AppendLine("public enum State { First, Second }");
            for (var model = 0; model < 10; model++)
            {
                source.AppendLine($"[TinyhandObject] public partial class Model{model} {{");
                source.AppendLine("[Key(0)] public int Id { get; set; } [Key(1)] public string Name { get; set; } = string.Empty;");
                source.AppendLine("[Key(2)] public long Count { get; set; } [Key(3)] public double Price { get; set; }");
                source.AppendLine(scenario == "Collections"
                    ? "[Key(4)] public State[] Values { get; set; } = []; [Key(5)] public List<State> Items { get; set; } = new();"
                    : "[Key(4)] public bool Enabled { get; set; } [Key(5)] public DateTime Created { get; set; }");
                source.AppendLine("}");
            }
        }

        yield return source.ToString();
    }
}

internal sealed record Measurement(string Scenario, string Mode, double MedianMilliseconds, long MedianAllocatedBytes, int OutputCharacters, string OutputHash);
