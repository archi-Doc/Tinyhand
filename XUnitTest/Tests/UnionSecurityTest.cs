// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class UnionSecurityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownDiscriminatorsAreRejectedWithoutChangingExistingValues(bool nilDiscriminator)
    {
        byte[] integerData = nilDiscriminator ? [0x81, 0xc0, 0xc0] : [0x81, 99, 0x90];
        byte[] stringData = nilDiscriminator ? [0x81, 0xc0, 0xc0] : [0x81, 0xa1, (byte)'?', 0x90];
        var original = new StrictUnionLeaf();
        CheckUnknown<IStrictOuterUnion>(integerData, original);
        CheckUnknown<IStrictStringUnion>(stringData, original);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryUnionWrapperParticipatesInTheDepthLimit(bool untrusted)
    {
        // Outer union -> inner union -> concrete object: three nested containers.
        byte[] data = [0x81, 1, 0x81, 2, 0x91, 0xc0];
        var security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData;
        var options = TinyhandSerializerOptions.Standard with { Security = security.WithMaximumObjectGraphDepth(2) };
        StrictUnionLeaf.Constructions = 0;
        Assert.Throws<InsufficientExecutionStackException>(() => Decode<IStrictOuterUnion>(data, options));
        Assert.Equal(0, StrictUnionLeaf.Constructions);

        Assert.IsType<StrictUnionLeaf>(Decode<IStrictOuterUnion>(data, options with { Security = security.WithMaximumObjectGraphDepth(3) }));
        Assert.Equal(1, StrictUnionLeaf.Constructions);
        Assert.Null(Decode<IStrictOuterUnion>([0xc0], options with { Security = security.WithMaximumObjectGraphDepth(0) }));

        // A malformed selected subtype must unwind both union depth increments.
        Assert.Throws<TinyhandUnexpectedCodeException>(() => Decode<IStrictOuterUnion>([0x81, 1, 0x81, 2, 0xc2], options with { Security = security.WithMaximumObjectGraphDepth(3) }));
    }

    private static void CheckUnknown<T>(byte[] data, T original)
        where T : class, ITinyhandSerializable<T>
    {
        T? value = original;
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        Assert.Throws<TinyhandException>(() =>
        {
            var reader = new TinyhandReader(data);
            try
            {
                TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
            }
            finally
            {
                Assert.Equal(0, reader.Depth);
            }
        });
        Assert.Same(original, value);

        foreach (var security in new[] { TinyhandSecurity.TrustedData, TinyhandSecurity.UntrustedData.WithRejectUnknownUnionTypes(false) })
        {
            value = original;
            var reader = new TinyhandReader(data);
            TinyhandSerializer.DeserializeObject(ref reader, ref value, options with { Security = security });
            Assert.Same(original, value);
            Assert.Equal(0, reader.Depth);
            Assert.True(reader.End);
        }
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
[TinyhandUnion(1, typeof(IStrictInnerUnion))]
public partial interface IStrictOuterUnion;

[TinyhandObject]
[TinyhandUnion(2, typeof(StrictUnionLeaf))]
public partial interface IStrictInnerUnion : IStrictOuterUnion;

[TinyhandUnion("known", typeof(StrictUnionLeaf))]
public partial interface IStrictStringUnion;

[TinyhandObject]
public partial class StrictUnionLeaf : IStrictInnerUnion, IStrictStringUnion
{
    public static int Constructions;

    public StrictUnionLeaf() => Constructions++;

    [Key(0)]
    public IStrictOuterUnion? Next { get; set; }
}
