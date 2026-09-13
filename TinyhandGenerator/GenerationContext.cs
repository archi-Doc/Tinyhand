// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Arc.Visceral;

namespace Tinyhand.Generator;

public class GenerationContext
{
    public GenerationContext(string? assemblyName)
    {
        var assemblyId = string.IsNullOrEmpty(assemblyName) ? string.Empty : VisceralHelper.AssemblyNameToIdentifier("_" + assemblyName);
        this.GeneratedClassName = "Generated" + assemblyId;
        this.GeneratedClassFullName = "global::Tinyhand.Formatters." + this.GeneratedClassName;
    }

    public string GeneratedClassFullName { get; }

    public string GeneratedClassName { get; }

    public Queue<TinyhandObject> FormatterGenerationQueue { get; } = new();

    public int FormatterCount { get; set; } = 0;

    public List<string> ModuleInitializerClasses { get; } = new();

    public bool GeneratingStaticMethod { get; set; }

    public bool EnumAsString { get; set; }

    public bool TryGetBlock(string blockKey, out GeneratorBlock block) => this.keyToBlock.TryGetValue(blockKey, out block);

    public bool GetOrCreateBlock(string blockKey, out GeneratorBlock block)
    {
        if (this.TryGetBlock(blockKey, out block))
        {// Already exists.
            return false;
        }

        // Create new block.
        block = new GeneratorBlock(blockKey, this.blockSerialNumber++);
        this.keyToBlock[blockKey] = block;
        return true;
    }

    public void AppendBlocks(ScopingStringBuilder ssb)
    {
        foreach (var x in this.keyToBlock.Values)
        {
            ssb.Append(x.Ssb);
        }
    }

    private int blockSerialNumber;
    private Dictionary<string, GeneratorBlock> keyToBlock = new();
}

public class GeneratorBlock
{
    public string BlockKey { get; }

    public int SerialNumber { get; }

    public ScopingStringBuilder Ssb { get; }

    public GeneratorBlock(string blockKey, int serialNumber)
    {
        this.BlockKey = blockKey;
        this.SerialNumber = serialNumber;
        this.Ssb = new();
    }
}
