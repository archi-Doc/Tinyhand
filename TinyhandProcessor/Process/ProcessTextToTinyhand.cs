// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tinyhand.Tree;

#pragma warning disable CS1998
#pragma warning disable SA1602

namespace Tinyhand;

public class TinyhandProcessCore_TextToTinyhand : IProcessCore
{
    public enum Format
    {
        Compressed,
        Binary,
        Utf8,
    }

    public static string StaticName => "text to tinyhand";

    public TinyhandProcessCore_TextToTinyhand()
    {
    }

    public IProcessEnvironment Environment { get; private set; } = default!;

    public string ProcessName => StaticName;

    public void Initialize(IProcessEnvironment environment)
    {
        this.Environment = environment;
    }

    public void Uninitialize()
    {
    }

    public async Task<bool> Process(Element element)
    {
        if (element.TryGetRight_Value_String("format", out var valueFile))
        {
            this.format = valueFile.ValueStringUtf16 switch
            {
                "binary" => Format.Binary,
                "utf8" => Format.Utf8,
                _ => Format.Compressed,
            };
        }
        else if (element is Value_String valueString)
        {
            var path = this.Environment.CombinePath(PathType.SourceFolder, valueString.ValueStringUtf16);
            if (!File.Exists(path))
            {
                this.Environment.Log.Error(element, $"\"{path}\" does not exists.");
                return false;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
                lines = lines.Where(x => x.Length != 0).ToArray();
            }
            catch
            {
                this.Environment.Log.Error(element, $"Could not read \"{path}\".");
                return false;
            }

            string path2 = string.Empty;
            try
            {
                path2 = Path.ChangeExtension(path, "tinyhand");

                byte[] output;

                if (this.format == Format.Binary)
                {
                    output = TinyhandSerializer.Serialize(lines);
                    // var lines2 = TinyhandSerializer.Deserialize<string[]>(output);
                }
                else if (this.format == Format.Utf8)
                {
                    output = TinyhandSerializer.SerializeToUtf8(lines);
                }
                else
                {
                    output = TinyhandSerializer.Serialize(lines, TinyhandSerializerOptions.Lz4);
                    // var lines2 = TinyhandSerializer.Deserialize<string[]>(output, TinyhandSerializerOptions.Lz4);
                }

                File.WriteAllBytes(path2, output);
            }
            catch
            {
                this.Environment.Log.Error(element, $"Could not write \"{path2}\".");
                return false;
            }

            this.Environment.Log.Information(null, $"Done: {path} => {path2}");
        }

        return true;
    }

    private Format format;
}
