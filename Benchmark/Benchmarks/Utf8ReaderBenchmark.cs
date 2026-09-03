// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.Tree;

namespace Benchmark;

/// <summary>
/// Measures the tokenizer (<see cref="TinyhandUtf8Reader"/>) throughput.<br/>
/// <c>Tokenize</c> is the reader alone, <c>Parse</c> additionally builds the <see cref="Element"/> tree,
/// so the difference between the two shows how much of the parsing cost belongs to the reader.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class Utf8ReaderBenchmark
{
    /// <summary>
    /// The shape of the document to read.
    /// </summary>
    public enum DocumentKind
    {
        /// <summary>
        /// identifier = "string" pairs (the shape of a language resource file).
        /// </summary>
        Strings,

        /// <summary>
        /// identifier = number pairs (long/ulong/double).
        /// </summary>
        Numbers,

        /// <summary>
        /// Nested groups, both braced and indented.
        /// </summary>
        Groups,

        /// <summary>
        /// Assignments interleaved with comments and blank lines.
        /// </summary>
        Comments,
    }

    private const int Repeat = 200;

    private byte[] utf8 = Array.Empty<byte>();

    [Params(DocumentKind.Strings, DocumentKind.Numbers, DocumentKind.Groups, DocumentKind.Comments)]
    public DocumentKind Kind { get; set; }

    /// <summary>
    /// Gets the size of the document in bytes (reported so that the throughput can be derived).
    /// </summary>
    public int DocumentSize => this.utf8.Length;

    [GlobalSetup]
    public void Setup()
    {
        this.utf8 = Encoding.UTF8.GetBytes(CreateDocument(this.Kind));
    }

    /// <summary>
    /// Reads every atom. This is the tokenizer alone; no tree is built.
    /// </summary>
    /// <returns>The number of atoms read, plus the total length of their values.</returns>
    [Benchmark]
    public int Tokenize()
    {
        var reader = new TinyhandUtf8Reader(this.utf8);
        var count = 0;
        while (reader.Read())
        {
            count += 1 + reader.ValueSpan.Length;
        }

        return count;
    }

    /// <summary>
    /// Reads every atom including the contextual information (comments and line feeds),
    /// which is the mode <see cref="TinyhandParser"/> uses.
    /// </summary>
    /// <returns>The number of atoms read, plus the total length of their values.</returns>
    [Benchmark]
    public int TokenizeContextual()
    {
        var reader = new TinyhandUtf8Reader(this.utf8, true);
        var count = 0;
        while (reader.Read())
        {
            count += 1 + reader.ValueSpan.Length;
        }

        return count;
    }

    /// <summary>
    /// Tokenizes and builds the <see cref="Element"/> tree.
    /// </summary>
    /// <returns>The root element.</returns>
    [Benchmark(Baseline = true)]
    public Element Parse()
        => TinyhandParser.Parse(this.utf8);

    /// <summary>
    /// Tokenizes and builds the <see cref="Element"/> tree, keeping the contextual information.
    /// </summary>
    /// <returns>The root element.</returns>
    [Benchmark]
    public Element ParseContextual()
        => TinyhandParser.Parse(this.utf8, TinyhandParserOptions.ContextualInformation);

    private static string CreateDocument(DocumentKind kind)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Repeat; i++)
        {
            switch (kind)
            {
                case DocumentKind.Strings:
                    sb.Append($"Identifier{i} = \"Value {i} with a moderately long text\"\n");
                    sb.Append($"Escaped{i} = \"Quote\\\" Tab\\t Unicode\\u3042\"\n");
                    break;

                case DocumentKind.Numbers:
                    sb.Append($"Long{i} = {-i * 1000}\n");
                    sb.Append($"ULong{i} = {(ulong)i * 100000000000}\n");
                    sb.Append($"Double{i} = {i}.{i}\n");
                    sb.Append($"Bool{i} = {(i % 2 == 0 ? "true" : "false")}\n");
                    sb.Append($"Null{i} = null\n");
                    break;

                case DocumentKind.Groups:
                    sb.Append($"Braced{i} = {{ A = {i}, B = \"b\", C = {{ D = true }} }}\n");
                    sb.Append($"Indented{i} =\n  A = {i}\n  B = \"b\"\n  C =\n    D = true\n");
                    break;

                case DocumentKind.Comments:
                    sb.Append($"// Line comment {i}\n");
                    sb.Append($"Identifier{i} = {i} /* block comment */ # sharp comment\n");
                    sb.Append('\n');
                    break;
            }
        }

        return sb.ToString();
    }
}
