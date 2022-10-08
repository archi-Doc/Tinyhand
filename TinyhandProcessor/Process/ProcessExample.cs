// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;
using Tinyhand.Tree;

#pragma warning disable CS1998

namespace Tinyhand;

public class TinyhandProcessCore_Example : IProcessCore
{
    public static string StaticName => "example";

    public TinyhandProcessCore_Example()
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
        if (element.TryGetRight_Value_Long("number", out var valueLong))
        {
            this.number = (int)valueLong.ValueLong;
        }
        else if (element.TryGetRight_Value_String("file", out var valueFile))
        {
            this.file = valueFile.ValueStringUtf16;
        }
        else if (element is Value_String valueString)
        {
            /*var path = this.Environment.CombinePath(PathType.SourceFolder, valueString.ValueStringUtf16);
            var logpath = valueString.ValueStringUtf16;
            if (!File.Exists(path))
            {
                this.Environment.Log.Error(element, $"\"{path}\" does not exists.");
                return false;
            }*/

            this.Environment.Log.Information(null, $"{valueString.ValueStringUtf16}");
        }

        return true;
    }

    private int number = 1;
    private string file = string.Empty;
}
