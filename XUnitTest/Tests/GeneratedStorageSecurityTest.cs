// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class GeneratedStorageSecurityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedCollectionsRejectAmplifiedStorageBeforeAllocation(bool list)
    {
        Assert.Equal(4096, Unsafe.SizeOf<GeneratedStorageBlock>());
        var data = CreateContainer(8);
        var options = Options(16 * 1024);
        if (list)
        {
            GeneratedStorageListModel? value = new() { Values = [new GeneratedStorageBlock { Value = 7 }] };
            var original = value.Values;
            CheckRejectedAllocation(() =>
            {
                var reader = new TinyhandReader(data);
                TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
            });
            Assert.Same(original, value.Values);
            Assert.Equal(7, value.Values[0].Value);
            Assert.Equal(4, Decode<GeneratedStorageListModel>(CreateContainer(4), options)!.Values.Count);
        }
        else
        {
            GeneratedStorageArrayModel? value = new() { Values = [new GeneratedStorageBlock { Value = 7 }] };
            var original = value.Values;
            CheckRejectedAllocation(() =>
            {
                var reader = new TinyhandReader(data);
                TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
            });
            Assert.Same(original, value.Values);
            Assert.Equal(7, value.Values[0].Value);
            Assert.Equal(4, Decode<GeneratedStorageArrayModel>(CreateContainer(4), options)!.Values.Length);
        }
    }

    [Fact]
    public void GeneratedArraysAndListsShareTheCumulativeStorageBudget()
    {
        byte[] data = [0x92, 0x92, 0x90, 0x90, 0x92, 0x90, 0x90];
        var accepted = Decode<GeneratedStorageMixedModel>(data, Options(16 * 1024))!;
        Assert.Equal(2, accepted.Array.Length);
        Assert.Equal(2, accepted.List.Count);
        Assert.Throws<TinyhandException>(() => Decode<GeneratedStorageMixedModel>(data, Options((16 * 1024) - 1)));
    }

    private static TinyhandSerializerOptions Options(long storageBytes)
        => TinyhandSerializerOptions.Standard with
        {
            Security = TinyhandSecurity.UntrustedData.WithLimits(TinyhandDeserializationLimits.UntrustedData with
            {
                MaximumTotalCollectionBytes = storageBytes,
            }),
        };

    private static byte[] CreateContainer(int count)
    {
        var data = new byte[count + 2];
        data[0] = 0x91;
        data[1] = (byte)(0x90 + count);
        data.AsSpan(2).Fill(0x90);
        return data;
    }

    private static void CheckRejectedAllocation(Action deserialize)
    {
        Assert.Throws<TinyhandException>(deserialize);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<TinyhandException>(deserialize);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 16 * 1024, "The rejected 32 KiB backing store must not be allocated.");
    }

    private static T? Decode<T>(byte[] data, TinyhandSerializerOptions options)
        where T : class, ITinyhandSerializable<T>
    {
        var reader = new TinyhandReader(data);
        T? value = null;
        try
        {
            TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
            Assert.True(reader.End);
            return value;
        }
        finally
        {
            Assert.Equal(0, reader.Depth);
        }
    }
}

[TinyhandObject]
[StructLayout(LayoutKind.Sequential, Size = 4096)]
public partial struct GeneratedStorageBlock
{
    [Key(0)]
    public int Value;
}

[TinyhandObject]
public partial class GeneratedStorageArrayModel
{
    [Key(0)]
    public GeneratedStorageBlock[] Values { get; set; } = [];
}

[TinyhandObject]
public partial class GeneratedStorageListModel
{
    [Key(0)]
    public List<GeneratedStorageBlock> Values { get; set; } = [];
}

[TinyhandObject]
public partial class GeneratedStorageMixedModel
{
    [Key(0)]
    public GeneratedStorageBlock[] Array { get; set; } = [];

    [Key(1)]
    public List<GeneratedStorageBlock> List { get; set; } = [];
}
