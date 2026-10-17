// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Tree;
using Xunit;

namespace XUnitTest.Tests;

public class TreeCollectionStorageSecurityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TreeContainersHonorExactAndCumulativeStorageBudgets(bool untrusted)
    {
        CheckBoundary([0x92, 1, 2], 2L * IntPtr.Size, untrusted);
        CheckBoundary([0x81, 1, 2], 2L * IntPtr.Size, untrusted);
        CheckBoundary([0x92, 0x91, 1, 0x81, 2, 3], 5L * IntPtr.Size, untrusted);

        var options = CreateOptions(0, untrusted);
        TinyhandTreeConverter.FromBinaryToElement([0x90], out var emptyArray, options);
        Assert.Empty(Assert.IsType<Group>(emptyArray).ElementList);
        TinyhandTreeConverter.FromBinaryToElement([0x80], out var emptyMap, options);
        Assert.Empty(Assert.IsType<Group>(emptyMap).ElementList);
    }

    [Fact]
    public void TreeStorageRejectionPrecedesBackingAllocationAndRestoresDepth()
    {
        var data = new byte[16_387];
        data.AsSpan().Fill(MessagePackCode.Nil);
        data[0] = MessagePackCode.Array16;
        data[1] = 0x40;
        data[2] = 0;
        var options = CreateOptions(0, true);
        Action deserialize = () =>
        {
            var reader = new TinyhandReader(data);
            try
            {
                TinyhandTreeConverter.FromReaderToElement(ref reader, out _, options);
            }
            finally
            {
                Assert.Equal(0, reader.Depth);
            }
        };

        Assert.Throws<TinyhandException>(deserialize);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<TinyhandException>(deserialize);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 64 * 1024, "Reject the storage request before allocating the tree's backing list.");
    }

    private static void CheckBoundary(byte[] data, long storageBytes, bool untrusted)
    {
        var rejected = CreateOptions(storageBytes - 1, untrusted);
        Assert.Throws<TinyhandException>(() => TinyhandTreeConverter.FromBinaryToElement(data, out _, rejected));

        var accepted = CreateOptions(storageBytes, untrusted);
        TinyhandTreeConverter.FromBinaryToElement(data, out var element, accepted);
        TinyhandTreeConverter.FromElementToBinary(element, out var roundTrip, accepted);
        Assert.Equal(data, roundTrip);
    }

    private static TinyhandSerializerOptions CreateOptions(long storageBytes, bool untrusted)
    {
        var security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData;
        return TinyhandSerializerOptions.Standard with
        {
            Security = security.WithLimits(security.Limits with { MaximumTotalCollectionBytes = storageBytes }),
        };
    }
}
