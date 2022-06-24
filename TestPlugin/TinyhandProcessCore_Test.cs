// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using System.Threading.Tasks;
using Tinyhand.Tree;

#pragma warning disable CS1998
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand;

public class TinyhandProcessCore_Test : IProcessCore
{
    public static string StaticName => "log test";

    public TinyhandProcessCore_Test()
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
        this.Environment.Log.Information(element, "Log test.");
        this.Environment.Result.Error(element, "Error test.");

        return true;
    }
}
