// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class TemplateBenchmark
{

    public TemplateBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public bool CompareTrue()
    {
        return true;
    }
}
