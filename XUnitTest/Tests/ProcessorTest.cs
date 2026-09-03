// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.Tree;
using Xunit;

namespace XUnitTest.Tests;

public class ProcessorTest : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("Tinyhand.ProcessorTest.").FullName;

    [Theory]
    [InlineData("binary")]
    [InlineData("utf8")]
    [InlineData("compressed")]
    public async Task TextConversionPreservesNonemptyLines(string format)
    {
        var path = Path.Combine(this.directory, "source.txt");
        File.WriteAllText(path, "first\n\n日本語\n \nlast\n", Encoding.UTF8);
        Assert.True(await this.Run($$"""
            process = "text to tinyhand"
            format = "{{format}}"
            "source.txt"
            """));

        var bytes = File.ReadAllBytes(Path.ChangeExtension(path, "tinyhand"));
        var lines = format == "utf8" ? TinyhandSerializer.DeserializeFromUtf8<string[]>(bytes) :
            TinyhandSerializer.Deserialize<string[]>(bytes, format == "binary" ? TinyhandSerializerOptions.Standard : TinyhandSerializerOptions.Lz4);
        Assert.Equal(new[] { "first", "日本語", " ", "last" }, lines);
    }

    [Fact]
    public async Task LanguageMergePreservesNullCharactersAndNullValues()
    {
        File.WriteAllText(Path.Combine(this.directory, "reference.tinyhand"), "a = \"base\" b = null c = \"base\" nested = { d = \"fallback\" e = \"unchanged\" }");
        File.WriteAllText(Path.Combine(this.directory, "target.tinyhand"), "a = \"\\u0000\" b = \"\\u0000\" c = null nested = { d = \"翻訳\" }");
        Assert.True(await this.Run("""
            destination = "output"
            process = "language file"
            reference = "reference.tinyhand"
            "target.tinyhand"
            """));

        var output = (Group)TinyhandParser.ParseFile(Path.Combine(this.directory, "output", "target.tinyhand"));
        Assert.Equal("\0", Assert.IsType<Value_String>(Assert.IsType<Assignment>(output.ElementList[0]).RightElement).Utf16);
        Assert.Equal("\0", Assert.IsType<Value_String>(Assert.IsType<Assignment>(output.ElementList[1]).RightElement).Utf16);
        Assert.IsType<Value_Null>(Assert.IsType<Assignment>(output.ElementList[2]).RightElement);
        var nested = Assert.IsType<Group>(Assert.IsType<Assignment>(output.ElementList[3]).RightElement);
        Assert.Equal("翻訳", Assert.IsType<Value_String>(Assert.IsType<Assignment>(nested.ElementList[0]).RightElement).Utf16);
        Assert.Equal("unchanged", Assert.IsType<Value_String>(Assert.IsType<Assignment>(nested.ElementList[1]).RightElement).Utf16);
    }

    [Fact]
    public async Task LanguageWriteFailureIsReported()
    {
        File.WriteAllText(Path.Combine(this.directory, "reference.tinyhand"), "key = \"value\"");
        Directory.CreateDirectory(Path.Combine(this.directory, "output", "target.tinyhand"));
        Assert.False(await this.Run("""
            destination = "output"
            process = "language file"
            reference = "reference.tinyhand"
            "target.tinyhand"
            """));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidLanguageReferenceReturnsFailure(bool malformed)
    {
        if (malformed)
        {
            File.WriteAllText(Path.Combine(this.directory, "reference.tinyhand"), "{");
        }

        using var environment = new ProcessEnvironment((Group)TinyhandParser.Parse("mode = \"process\" log = \"\""u8), Path.Combine(this.directory, "process.tinyhand"));
        var core = new TinyhandProcessCore_LanguageFile();
        core.Initialize(environment);
        var assignment = ((Group)TinyhandParser.Parse("reference = \"reference.tinyhand\""u8)).ElementList[0];
        Assert.False(await core.Process(assignment));
        Assert.True(environment.FatalStatus);
    }

    [Theory]
    [InlineData("process = \"missing\"")]
    [InlineData("process = \"text to tinyhand\" \"missing.txt\"")]
    [InlineData("process = \"language file\" \"target.tinyhand\"")]
    [InlineData("process = \"language file\" reference = 123")]
    public async Task InvalidProcessInputReturnsFailure(string script)
        => Assert.False(await this.Run(script));

    public void Dispose()
        => Directory.Delete(this.directory, recursive: true);

    private Task<bool> Run(string script)
        => TinyhandProcess.Process(TinyhandParser.Parse(Encoding.UTF8.GetBytes("mode = \"process\" log = \"\"\n" + script)), Path.Combine(this.directory, "process.tinyhand"));
}
