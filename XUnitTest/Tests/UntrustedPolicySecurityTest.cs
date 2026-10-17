// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class UntrustedPolicySecurityTest
{
    [Fact]
    public void PolicyCopiesPreserveIndependentProtections()
    {
        var policy = TinyhandSecurity.UntrustedData.WithHashCollisionResistant(false).WithMaximumObjectGraphDepth(42)
            .WithLimits(TinyhandDeserializationLimits.Unrestricted);
        Assert.True(policy.ValidateStringValues);
        Assert.True(policy.RejectDuplicateKeys);
        Assert.True(policy.RejectUnknownUnionTypes);
        Assert.True(policy.RequireCompleteInput);
        Assert.True(policy.CheckAvailableStack);
        Assert.True(policy.SortBeforeInsertion);
        Assert.True(policy.WithRejectDuplicateKeys(false).SortBeforeInsertion);
        Assert.False(policy.WithSortBeforeInsertion(false).SortBeforeInsertion);
        Assert.Same(policy, policy.WithSortBeforeInsertion(true));
        Assert.Same(policy, policy.WithCheckAvailableStack(true));
        Assert.False(policy.WithCheckAvailableStack(false).CheckAvailableStack);
        Assert.True(policy.WithRequireCompleteInput(false).CheckAvailableStack);
        Assert.True(policy.WithValidateStringValues(false).RejectDuplicateKeys);
        Assert.True(policy.WithRejectDuplicateKeys(false).RejectUnknownUnionTypes);
        Assert.True(policy.WithRejectUnknownUnionTypes(false).RequireCompleteInput);
        Assert.Equal(42, policy.WithCheckAvailableStack(false).MaximumObjectGraphDepth);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyhandDeserializationLimits { MaximumTotalCollectionBytes = -1 });
    }

    [Fact]
    public void CollectionStorageBudgetIsSharedByReaderCopiesAndSubreaders()
    {
        Assert.Throws<TinyhandException>(() => ExhaustSharedBudget());
    }

    [Fact]
    public void StackCheckRejectsBeforeIncrementingDepthWhenRuntimeReserveIsReached()
    {
        Exception? failure = null;
        var reachedReserve = false;
        var thread = new Thread(() =>
        {
            try
            {
                reachedReserve = ProbeStack(0);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        }, 1024 * 1024);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        Assert.Null(failure);
        Assert.True(reachedReserve);
    }

    private static void ExhaustSharedBudget()
    {
        var policy = TinyhandSecurity.UntrustedData.WithLimits(new TinyhandDeserializationLimits { MaximumTotalCollectionBytes = 8 });
        var reader = new TinyhandReader(new byte[] { MessagePackCode.Nil }, policy);
        reader.ReserveCollectionStorage<int>(1);
        var copy = reader.Fork();
        copy.ReserveCollectionStorage<int>(1);
        var child = reader.CreateSubReader(new byte[] { MessagePackCode.Nil });
        child.ApplySecurity(policy.WithLimits(TinyhandDeserializationLimits.Unrestricted));
        child.ReserveCollectionStorage<byte>(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProbeStack(int frame)
    {
        // Stop using the runtime probe even if the policy regresses, so this test never deliberately overflows the stack.
        Span<byte> padding = stackalloc byte[1024];
        padding[0] = (byte)frame;
        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            var reader = new TinyhandReader(ReadOnlySpan<byte>.Empty);
            try
            {
                TinyhandSecurity.UntrustedData.IncrementDepth(ref reader);
                return false;
            }
            catch (InsufficientExecutionStackException)
            {
                return reader.Depth == 0;
            }
        }

        var result = frame < 4096 && ProbeStack(frame + 1);
        return result && padding[0] == (byte)frame;
    }
}
