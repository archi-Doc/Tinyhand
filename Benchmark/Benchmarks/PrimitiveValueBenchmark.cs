// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class PrimitiveValueBenchmark
{
    private readonly TagObject tag0;
    private readonly TagObject tag1;
    private readonly TagObject tag2;
    private readonly TagObject tag3;
    private readonly TagObject tag4;
    private readonly TagObject tag5;
    private readonly TagObject tag6;
    private readonly TagObject targetTag;
    private readonly object targetObject;
    private int x = 123456789;

    public PrimitiveValueBenchmark()
    {
        this.tag0 = TagObject.FromTag(0);
        this.tag1 = TagObject.FromTag(1);
        this.tag2 = TagObject.FromTag(2);
        this.tag3 = TagObject.FromTag(3);
        this.tag4 = TagObject.FromTag(4);
        this.tag5 = TagObject.FromTag(5);
        this.tag6 = TagObject.FromTag(6);

        this.targetTag = TagObject.FromTag(6);
        this.targetObject = this.targetTag;
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public int GetHash()
    {
        var v = new PrimitiveValue(this.x);
        return v.GetHashCode();
    }

    [Benchmark]
    public int GetHash2()
    {
        var v = new PrimitiveValue(this.x);
        return v.GetHashCode2();
    }

    [Benchmark]
    public bool Equal()
    {
        var v = new PrimitiveValue(this.x);
        var v2 = new PrimitiveValue(this.x);
        return v.Equals(v2);
    }

    [Benchmark]
    public bool Equal2()
    {
        var v = new PrimitiveValue(this.x);
        var v2 = new PrimitiveValue(this.x);
        return v.Equals2(v2);
    }

    /*[Benchmark]
    public int Find()
    {
        if (this.targetTag == this.tag0)
        {
            return 0;
        }
        else if (this.targetTag == this.tag1)
        {
            return 1;
        }
        else if (this.targetTag == this.tag2)
        {
            return 2;
        }
        else if (this.targetTag == this.tag3)
        {
            return 3;
        }
        else if (this.targetTag == this.tag4)
        {
            return 4;
        }
        else if (this.targetTag == this.tag5)
        {
            return 5;
        }
        else if (this.targetTag == this.tag6)
        {
            return 6;
        }

        return -1;
    }

    [Benchmark]
    public int Find2()
    {
        for (var i = 0; i < TagObject.MaxTag; i++)
        {
            if (this.targetTag == TagObject.FromTag(i))
            {
                return i;
            }
        }

        return -1;
    }

    [Benchmark]
    public int GetTag()
    {
        return this.targetTag.Tag;
    }

    [Benchmark]
    public int GetTag2()
    {
        return TagObject.ToTag(this.targetTag);
    }

    [Benchmark]
    public int GetTag3()
    {
        return TagObject.ToTag(this.targetObject);
    }*/
}
