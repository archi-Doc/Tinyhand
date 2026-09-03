// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.Tree;

if (RuntimeFeature.IsDynamicCodeSupported)
{
    throw new InvalidOperationException("This test must run as a NativeAOT executable.");
}

TinyhandProcessCore_Test.Register();
TinyhandProcess.RegisterPlugin<FailingPlugin>("failure");
TinyhandProcess.RegisterPlugin<ThrowingPlugin>("constructor failure");
var directory = Directory.CreateTempSubdirectory("Tinyhand.NativeAot.").FullName;
try
{
    foreach (var mode in new[] { "", "console", "file", "both" })
    {
        var logPath = Path.Combine(directory, mode + "-log.txt");
        var resultPath = Path.Combine(directory, mode + "-result.txt");
        var source = $$"""
            mode = "process"
            log = {{Output(mode, logPath)}}
            result = {{Output(mode, resultPath)}}
            process = "log test"
            "message"
            """;
        if (!await TinyhandProcess.Process(TinyhandParser.Parse(Encoding.UTF8.GetBytes(source)), null))
        {
            throw new InvalidOperationException("plugin process failed");
        }

        if (mode is "file" or "both")
        {
            // Arc.Unit inserts the date before the configured extension.
            if (!ContainsLog(mode + "-log*.txt", "Log test.") || !ContainsLog(mode + "-result*.txt", "Error test."))
            {
                throw new InvalidOperationException("file logger output missing");
            }
        }
    }

    foreach (var name in new[] { "unknown", "failure", "constructor failure" })
    {
        var source = $$"""
            mode = "process"
            log = ""
            process = "{{name}}"
            "message"
            """;
        if (await TinyhandProcess.Process(TinyhandParser.Parse(Encoding.UTF8.GetBytes(source)), null))
        {
            throw new InvalidOperationException("process failure was not propagated");
        }
    }
}
finally
{
    foreach (var file in Directory.EnumerateFiles(directory))
    {
        File.Delete(file);
    }

    Directory.Delete(directory);
}

Console.WriteLine("NativeAOT processor checks passed.");

bool ContainsLog(string pattern, string message)
    => Directory.EnumerateFiles(directory, pattern).Any(file => File.ReadAllText(file).Contains(message, StringComparison.Ordinal));

static string Output(string mode, string path) => mode is "file" or "both"
    ? $$"""{ "file", path="{{path.Replace('\\', '/')}}", console={{(mode == "both" ? "true" : "false")}} }"""
    : $"\"{mode}\"";

public class FailingPlugin : IProcessCore
{
    public string ProcessName => "failure";
    public void Initialize(IProcessEnvironment environment) { }
    public void Uninitialize() { }
    public Task<bool> Process(Element element) => Task.FromResult(false);
}

public sealed class ThrowingPlugin : FailingPlugin
{
    public ThrowingPlugin() => throw new InvalidOperationException("constructor failure");
}
