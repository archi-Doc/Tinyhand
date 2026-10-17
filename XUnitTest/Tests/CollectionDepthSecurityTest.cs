// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Tinyhand;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class CollectionDepthSecurityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArcCollectionsCheckDepthBeforeChangingTheValue(bool untrusted)
    {
        var security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData;
        CheckValue(new OrderedMap<long, long> { { 1, 2 } }, new OrderedMapFormatter<long, long>(), security);
        CheckValue(new OrderedMultiMap<long, long> { { 1, 2 } }, new OrderedMultiMapFormatter<long, long>(), security);
        CheckValue(new OrderedSet<long> { 1 }, new OrderedSetFormatter<long>(), security);
        CheckValue(new OrderedMultiSet<long> { 1 }, new OrderedMultiSetFormatter<long>(), security);
        CheckValue(new UnorderedMap<long, long>(0, security.GetEqualityComparer<long>()) { { 1, 2 } }, new UnorderedMapFormatter<long, long>(), security);
        CheckValue(new UnorderedSet<long>(0, security.GetEqualityComparer<long>(), false) { 1 }, new UnorderedSetFormatter<long>(), security);
        CheckValue(new OrderedList<long> { 1 }, new OrderedListFormatter<long>(), security);
        CheckValue(new UnorderedList<long> { 1 }, new UnorderedListFormatter<long>(), security);
        CheckValue(new KeyValueList<long, long> { new(1, 2) }, new KeyValueListFormatter<long, long>(), security);

        var linkedList = new UnorderedLinkedList<long>();
        linkedList.AddLast(1);
        CheckValue(linkedList, new UnorderedLinkedListFormatter<long>(), security);
    }

    [Fact]
    public void Utf16HashtableChecksDepthBeforeChangingTheValue()
    {
        var table = new Utf16Hashtable<long>();
        table.AddOrUpdate("key", 1);
        CheckValue(table, new Utf16HashtableFormatter<long>(), TinyhandSecurity.TrustedData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectedArcCollectionsDoNotAllocateTheirDeclaredCapacity(bool untrusted)
    {
        var security = (untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData).WithMaximumObjectGraphDepth(0);
        var options = TinyhandSerializerOptions.Standard with { Security = security };
        var array = new byte[16_387];
        array[0] = MessagePackCode.Array16;
        array[1] = 0x40;
        var map = new byte[32_771];
        map[0] = MessagePackCode.Map16;
        map[1] = 0x40;

        CheckAllocation(new OrderedListFormatter<long>(), array, options);
        CheckAllocation(new UnorderedListFormatter<long>(), array, options);
        CheckAllocation(new UnorderedSetFormatter<long>(), array, options);
        CheckAllocation(new UnorderedMapFormatter<long, long>(), map, options);
        CheckAllocation(new KeyValueListFormatter<long, long>(), map, options);
    }

    private static void CheckValue<T>(T original, ITinyhandFormatter<T> formatter, TinyhandSecurity security)
        where T : class
    {
        var options = TinyhandSerializerOptions.Standard with { Security = security.WithMaximumObjectGraphDepth(1) };
        var bytes = Serialize(original, formatter, options);
        foreach (var reuse in new[] { false, true })
        {
            T? value = reuse ? original : null;
            var reader = new TinyhandReader(bytes) { Depth = 1 };
            bool rejected = false;
            try
            {
                formatter.Deserialize(ref reader, ref value, options);
            }
            catch (InsufficientExecutionStackException)
            {
                rejected = true;
            }

            Assert.True(rejected);
            Assert.Equal(1, reader.Depth);
            Assert.Same(reuse ? original : null, value);
            Assert.Equal(bytes, Serialize(original, formatter, options));

            // The same value is accepted immediately below the depth boundary.
            reader = new TinyhandReader(bytes);
            formatter.Deserialize(ref reader, ref value, options);
            Assert.Equal(0, reader.Depth);
            Assert.True(reader.End);
            Assert.Equal(bytes, Serialize(value!, formatter, options));
        }
    }

    private static void CheckAllocation<T>(ITinyhandFormatter<T> formatter, byte[] bytes, TinyhandSerializerOptions options)
        where T : class
    {
        Action deserialize = () =>
        {
            var reader = new TinyhandReader(bytes);
            T? value = null;
            formatter.Deserialize(ref reader, ref value, options);
        };

        Assert.Throws<InsufficientExecutionStackException>(deserialize);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<InsufficientExecutionStackException>(deserialize);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 64 * 1024, "Depth rejection must precede collection capacity allocation.");
    }

    private static byte[] Serialize<T>(T value, ITinyhandFormatter<T> formatter, TinyhandSerializerOptions options)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            formatter.Serialize(ref writer, value, options);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }
}
