// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class JournalBoundaryTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void DisabledJournalDoesNotWriteLocatorsOrSubmitRecords(int depth)
    {
        var root = new DisabledRoot();
        IStructuralObject node = new Node { StructuralRoot = root };
        for (var i = 0; i < depth; i++)
        {
            node = new Node { StructuralParent = node, StructuralRoot = root, StructuralKey = i };
        }

        Assert.False(node.TryGetJournalWriter(out _, out var writer, includeCurrent: false));
        writer.Dispose();
        Assert.False(node.TryGetJournalWriter(out _, out writer));
        writer.Dispose();
        node.AddJournalRecord(JournalRecord.Value);
        Assert.Equal(0, root.Submitted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void JournalLocatorsFollowRootToLeafOrder(int depth)
    {
        var root = new JournalTester();
        IStructuralObject node = new Node { StructuralRoot = root };
        for (var i = 0; i < depth; i++)
        {
            node = new Node { StructuralParent = node, StructuralRoot = root, StructuralKey = i };
        }

        foreach (var includeCurrent in new[] { false, true })
        {
            Assert.True(node.TryGetJournalWriter(out var actualRoot, out var writer, includeCurrent));
            try
            {
                Assert.Same(root, actualRoot);
                var reader = new TinyhandReader(writer.FlushAndGetArray());
                Assert.True(reader.TryReadJournal(out _, out var type));
                Assert.Equal(JournalType.Record, type);
                var count = includeCurrent ? depth : System.Math.Max(0, depth - 1);
                for (var i = 0; i < count; i++)
                {
                    reader.Read_Key();
                    Assert.Equal(i, reader.ReadInt32());
                }

                Assert.True(reader.End);
            }
            finally
            {
                writer.Dispose();
            }
        }
    }

    [Fact]
    public void RecordCannotReadBytesFromTheFollowingHeader()
    {
        var node = new Node();
        byte[] journal = [0, 0, 1, (byte)JournalType.Record, MessagePackCode.UInt8, 0, 0, 1, (byte)JournalType.Record, 42];
        Assert.False(JournalHelper.ReadJournal(node, journal));
        Assert.Equal(new[] { 42 }, node.Values);
    }

    [Fact]
    public void TruncatedPayloadIsRejectedBeforeInvokingTheObject()
    {
        var node = new Node();
        byte[] journal = [0, 0, 2, (byte)JournalType.Record, 42];
        Assert.False(JournalHelper.ReadJournal(node, journal));
        Assert.Empty(node.Values);
    }

    [Fact]
    public void UnknownRecordsAreSkippedAndTruncatedHeadersAreRejected()
    {
        var node = new Node();
        byte[] journal = [0, 0, 1, 255, 0xc1, 0, 0, 1, (byte)JournalType.Record, 42];
        Assert.True(JournalHelper.ReadJournal(node, journal));
        Assert.Equal(new[] { 42 }, node.Values);
        Assert.False(JournalHelper.ReadJournal(node, new byte[] { 0, 0, 1 }));
        Assert.True(JournalHelper.ReadJournal(node, System.Array.Empty<byte>()));
    }

    private sealed class Node : IStructuralObject
    {
        public IStructuralRoot? StructuralRoot { get; set; }

        public IStructuralObject? StructuralParent { get; set; }

        public int StructuralKey { get; set; }

        public List<int> Values { get; } = new();

        public bool ProcessJournalRecord(ref TinyhandReader reader)
        {
            this.Values.Add(reader.ReadInt32());
            return true;
        }
    }

    private sealed class DisabledRoot : IStructuralRoot
    {
        public int Submitted { get; private set; }

        public bool TryGetJournalWriter(JournalType recordType, out TinyhandWriter writer)
        {
            writer = default;
            return false;
        }

        public ulong AddJournalAndDispose(ref TinyhandWriter writer)
        {
            this.Submitted++;
            writer.Dispose();
            return 0;
        }

        public void AddToSaveQueue(int delaySeconds = 0)
        {
        }
    }
}
