// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark.TypeSwitch;

public class BaseClass
{
    public int X { get; set; }
}

public class ClassA : BaseClass
{
    public string A { get; set; } = string.Empty;
}

public class ClassB : BaseClass
{
    public string B { get; set; } = string.Empty;
}

public class ClassC : BaseClass
{
    public string C { get; set; } = string.Empty;
}

public class ClassD : BaseClass
{
    public string D { get; set; } = string.Empty;
}

public class ClassE : BaseClass
{
    public string E { get; set; } = string.Empty;
}

public class ClassF : BaseClass
{
    public string F { get; set; } = string.Empty;
}

public class ClassG : BaseClass
{
    public string G { get; set; } = string.Empty;
}

public class ClassH : BaseClass
{
    public string H { get; set; } = string.Empty;
}

public class ClassI : BaseClass
{
    public string I { get; set; } = string.Empty;
}

public class ClassJ : BaseClass
{
    public string J { get; set; } = string.Empty;
}

public class ClassK : BaseClass
{
    public string K { get; set; } = string.Empty;
}

public class ClassL : BaseClass
{
    public string L { get; set; } = string.Empty;
}

public class ClassM : BaseClass
{
    public string M { get; set; } = string.Empty;
}

public class ClassN : BaseClass
{
    public string N { get; set; } = string.Empty;
}

public class ClassO : BaseClass
{
    public string O { get; set; } = string.Empty;
}

[Config(typeof(BenchmarkConfig))]
public class SwitchBenchmark
{
    private BaseClass testClass;

    private ThreadsafeTypeKeyHashTable<Func<BaseClass, int>> table;

    public SwitchBenchmark()
    {
        this.testClass = new ClassN();
        this.table = new();
        this.table.TryAdd(typeof(ClassA), static x => x.X);
        this.table.TryAdd(typeof(ClassB), static x => x.X);
        this.table.TryAdd(typeof(ClassC), static x => x.X);
        this.table.TryAdd(typeof(ClassD), static x => x.X);
        this.table.TryAdd(typeof(ClassE), static x => x.X);
        this.table.TryAdd(typeof(ClassF), static x => x.X);
        this.table.TryAdd(typeof(ClassG), static x => x.X);
        this.table.TryAdd(typeof(ClassH), static x => x.X);
        this.table.TryAdd(typeof(ClassI), static x => x.X);
        this.table.TryAdd(typeof(ClassJ), static x => x.X);
        this.table.TryAdd(typeof(ClassK), static x => x.X);
        this.table.TryAdd(typeof(ClassL), static x => x.X);
        this.table.TryAdd(typeof(ClassM), static x => x.X);
        this.table.TryAdd(typeof(ClassN), static x => x.X);
        this.table.TryAdd(typeof(ClassO), static x => x.X);
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public string Switch4_Is()
    {
        switch (this.testClass)
        {
            case ClassA a:
                return a.A;

            case ClassB b:
                return b.B;

            case ClassC c:
                return c.C;

            case ClassD d:
                return d.D;
        }

        return string.Empty;
    }

    [Benchmark]
    public string Switch4_Type()
    {
        var type = this.testClass.GetType();
        if (type == typeof(ClassA))
        {
            return ((ClassA)this.testClass).A;
        }
        else if (type == typeof(ClassB))
        {
            return ((ClassB)this.testClass).B;
        }
        else if (type == typeof(ClassC))
        {
            return ((ClassC)this.testClass).C;
        }
        else if (type == typeof(ClassD))
        {
            return ((ClassD)this.testClass).D;
        }

        return string.Empty;
    }

    [Benchmark]
    public int Table15_Is()
    {
        if (this.table.TryGetValue(this.testClass.GetType(), out var func))
        {
            return func(this.testClass);
        }
        else
        {
            return 0;
        }
    }

    [Benchmark]
    public string Switch15_Type()
    {
        var type = this.testClass.GetType();
        if (type == typeof(ClassA))
        {
            return ((ClassA)this.testClass).A;
        }
        else if (type == typeof(ClassB))
        {
            return ((ClassB)this.testClass).B;
        }
        else if (type == typeof(ClassC))
        {
            return ((ClassC)this.testClass).C;
        }
        else if (type == typeof(ClassD))
        {
            return ((ClassD)this.testClass).D;
        }
        else if (type == typeof(ClassE))
        {
            return ((ClassE)this.testClass).E;
        }
        else if (type == typeof(ClassF))
        {
            return ((ClassF)this.testClass).F;
        }
        else if (type == typeof(ClassG))
        {
            return ((ClassG)this.testClass).G;
        }
        else if (type == typeof(ClassH))
        {
            return ((ClassH)this.testClass).H;
        }
        else if (type == typeof(ClassI))
        {
            return ((ClassI)this.testClass).I;
        }
        else if (type == typeof(ClassJ))
        {
            return ((ClassJ)this.testClass).J;
        }
        else if (type == typeof(ClassK))
        {
            return ((ClassK)this.testClass).K;
        }
        else if (type == typeof(ClassL))
        {
            return ((ClassL)this.testClass).L;
        }
        else if (type == typeof(ClassM))
        {
            return ((ClassM)this.testClass).M;
        }
        else if (type == typeof(ClassN))
        {
            return ((ClassN)this.testClass).N;
        }
        else if (type == typeof(ClassO))
        {
            return ((ClassO)this.testClass).O;
        }

        return string.Empty;
    }
}
