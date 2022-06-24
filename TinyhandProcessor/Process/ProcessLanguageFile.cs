// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tinyhand.Tree;

#pragma warning disable CS1998
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand;

public class TinyhandProcessCore_LanguageFile : IProcessCore
{
    public static string StaticName => "language file";

    public string ProcessName => StaticName;

    public IProcessEnvironment Environment { get; private set; } = default!;

    public void Initialize(IProcessEnvironment environment)
    {
        this.Environment = environment;
    }

    public void Uninitialize()
    {
    }

    public async Task<bool> Process(Element element)
    {
        if (element.TryGetRight_Value("reference", out var value))
        {
            if (value is Value_String s)
            {
                return this.ProcessReference(s);
            }
            else
            {
                this.Environment.Log.Fatal(value, "reference value is invalid.");
                return false;
            }
        }
        else if (element is Value_String s)
        {
            return this.ProcessTarget(s);
        }

        return true;
    }

    private static byte[] zeroByte = new byte[] { 0 };

    private Group? referenceGroup;

    private bool ProcessReference(Value_String valueString)
    {
        var referencePath = Path.Combine(this.Environment.GetPath(PathType.SourceFolder), valueString.ValueStringUtf16);
        if (!File.Exists(referencePath))
        {
            this.Environment.Log.Fatal(valueString, $"The reference file ({referencePath}) does not exist.");
        }

        try
        {
            this.referenceGroup = (Group)TinyhandParser.ParseFile(referencePath, TinyhandParserOptions.ContextualInformation);
        }
        catch (Exception e)
        {
            this.Environment.Log.Fatal(valueString, $"Could not parse the reference file ({referencePath}).");
            this.Environment.Log.Fatal(null, e.Message);
        }

        return true;
    }

    private bool ProcessTarget(Value_String targetElement)
    {
        if (this.referenceGroup == null)
        {
            this.Environment.Log.Error(targetElement, $"Reference file required.");
            return false;
        }

        // Load the target file.
        var targetPath = Path.Combine(this.Environment.GetPath(PathType.SourceFolder), targetElement.ValueStringUtf16);
        var destinationPath = Path.Combine(this.Environment.GetPath(PathType.DestinationFolder), targetElement.ValueStringUtf16);
        Group targetGroup;
        if (!File.Exists(targetPath))
        {
            this.Environment.Log.Warning(targetElement, $"The target file ({targetPath}) does not exist. An empty file is created.");
            targetGroup = Group.Empty;
            goto AddToTable;
        }

        // Parse.
        try
        {
            targetGroup = (Group)TinyhandParser.ParseFile(targetPath, TinyhandParserOptions.ContextualInformation);
        }
        catch (Exception e)
        {
            this.Environment.Log.Error(targetElement, $"Could not parse the target file ({targetPath}).");
            this.Environment.Log.Error(null, e.Message);
            return false;
        }

AddToTable:
// Add strings to Utf8Hashtable.
        var table = new Utf8Hashtable<byte[]>();
        foreach (var x in targetGroup)
        {
            if (x.TryGetLeft_IdentifierUtf8(out var identifier))
            {
                if (x.TryGetRight_Value(out var value))
                {
                    if (value is Value_String valueString)
                    { // String
                        table.TryAdd(identifier, valueString.ValueStringUtf8);
                    }
                    else if (value is Value_Null)
                    {
                        table.TryAdd(identifier, zeroByte);
                    }
                }
            }
        }

        var group = (Group)this.referenceGroup.DeepCopy();
        foreach (var x in group)
        {
            if (x.TryGetLeft_IdentifierUtf8(out var identifier))
            {
                if (x.TryGetRight_Value_String(out var valueString))
                {
                    var assignment = (Assignment)x;
                    if (table.TryGetValue(identifier, out var targetUtf8))
                    { // Found.
                        if (targetUtf8.Length == 1 && targetUtf8[0] == 0)
                        { // null
                            assignment.RightElement = new Value_Null(assignment.RightElement);
                        }
                        else
                        { // "string"
                            valueString.ValueStringUtf8 = targetUtf8;
                        }
                    }
                    else
                    { // Not found.
                        // valueString.ValueStringUtf8 = Array.Empty<byte>();

                        // assignment.RightElement?.MoveContextualChainTo(assignment);
                        // assignment.RightElement = null; // new Value_Null(assignment.RightElement);
                    }
                }
            }
        }

        try
        {
            if (Path.GetDirectoryName(destinationPath) is { } destinationFolder)
            {
                Directory.CreateDirectory(destinationFolder);
                using var fs = new FileStream(destinationPath, FileMode.Create);
                fs.Write(TinyhandComposer.Compose(group, TinyhandComposeOption.UseContextualInformation));
            }
        }
        catch
        {
            this.Environment.Log.Error(targetElement, $"Could not write the destination file ({destinationPath}).");
        }

        return true;
    }
}
