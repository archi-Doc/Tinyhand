// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

[Collection("SerializerDefaults")]
public class GeneratedConstructionSecurityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedObjectsValidateBeforeConstructingOrInvokingCallbacks(bool untrusted)
    {
        var security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData;
        Check<ConstructionArrayModel>([0x90], [0x91, 42], security);
        Check<ConstructionMapModel>([0x80], [0x81, 0xa5, (byte)'V', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 42], security);
    }

    [Fact]
    public void ConstructorFailureRestoresReaderDepth()
    {
        ConstructionCounters.Reset();
        ConstructionCounters.ThrowOnConstruct = true;
        try
        {
            var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.TrustedData.WithMaximumObjectGraphDepth(1) };
            Assert.Throws<InvalidOperationException>(() => Decode<ConstructionArrayModel>([0x90], options));
            Assert.Equal(1, ConstructionCounters.Constructions);
            Assert.Equal(0, ConstructionCounters.Deserializing);
            Assert.Equal(0, ConstructionCounters.Deserialized);
        }
        finally
        {
            ConstructionCounters.Reset();
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ServiceProviderIsCalledAfterValidationForBothKeyFormats(bool untrusted, bool alternateKeys)
    {
        IServiceProvider? original = null;
        try
        {
            original = TinyhandSerializer.ServiceProvider;
        }
        catch (TinyhandException)
        {
            // An unset provider is a valid original state; restore it below as well.
        }

        var provider = new ConstructionServiceProvider();
        try
        {
            TinyhandSerializer.ServiceProvider = provider;
            var security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData;
            if (alternateKeys)
            {
                Check<ConstructionServiceModel>([0x80], [0x81, 0xa5, (byte)'V', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 42], security, TinyhandSerializerOptions.ConvertToString);
            }
            else
            {
                Check<ConstructionServiceModel>([0x90], [0x91, 42], security);
            }

            Assert.Equal(1, provider.Calls);
        }
        finally
        {
            TinyhandSerializer.ServiceProvider = original!;
        }
    }

    private static void Check<T>(byte[] empty, byte[] populated, TinyhandSecurity security, TinyhandSerializerOptions? format = null)
        where T : class, ITinyhandSerializable<T>, IConstructionProbe
    {
        ConstructionCounters.Reset();
        var options = (format ?? TinyhandSerializerOptions.Standard) with { Security = security.WithMaximumObjectGraphDepth(0) };
        Assert.Throws<InsufficientExecutionStackException>(() => Decode<T>(empty, options));
        AssertNoConstructionOrCallbacks();
        Assert.Null(Decode<T>([0xc0], options));
        AssertNoConstructionOrCallbacks();

        options = options with { Security = security.WithLimits(security.Limits with { MaximumCollectionLength = 0 }) };
        Assert.Throws<TinyhandException>(() => Decode<T>(populated, options));
        AssertNoConstructionOrCallbacks();

        options = options with { Security = security.WithMaximumObjectGraphDepth(1) };
        Assert.Throws<TinyhandUnexpectedCodeException>(() => Decode<T>([0xc2], options));
        AssertNoConstructionOrCallbacks();

        var value = Decode<T>(populated, options)!;
        Assert.Equal(42, value.Value);
        Assert.Equal(1, ConstructionCounters.Constructions);
        Assert.Equal(1, ConstructionCounters.Deserializing);
        Assert.Equal(1, ConstructionCounters.Deserialized);

        // Reused objects retain the existing lock behavior, and do not run their constructor again.
        value.RequireLock = true;
        var reader = new TinyhandReader(populated);
        T.Deserialize(ref reader, ref value, options);
        Assert.True(reader.End);
        Assert.Equal(0, reader.Depth);
        Assert.Equal(1, ConstructionCounters.Constructions);
        Assert.Equal(2, ConstructionCounters.Deserializing);
        Assert.Equal(2, ConstructionCounters.Deserialized);
    }

    private static T? Decode<T>(byte[] bytes, TinyhandSerializerOptions options)
        where T : class, ITinyhandSerializable<T>
    {
        var reader = new TinyhandReader(bytes);
        T? value = null;
        try
        {
            T.Deserialize(ref reader, ref value, options);
            Assert.True(reader.End);
            return value;
        }
        finally
        {
            Assert.Equal(0, reader.Depth);
        }
    }

    private static void AssertNoConstructionOrCallbacks()
    {
        Assert.Equal(0, ConstructionCounters.Constructions);
        Assert.Equal(0, ConstructionCounters.Deserializing);
        Assert.Equal(0, ConstructionCounters.Deserialized);
    }

    private sealed class ConstructionServiceProvider : IServiceProvider
    {
        internal int Calls { get; private set; }

        public object? GetService(Type serviceType)
        {
            Assert.Equal(typeof(ConstructionServiceModel), serviceType);
            this.Calls++;
            return new ConstructionServiceModel();
        }
    }
}

public interface IConstructionProbe
{
    int Value { get; set; }

    bool RequireLock { get; set; }
}

internal static class ConstructionCounters
{
    internal static int Constructions;
    internal static int Deserializing;
    internal static int Deserialized;
    internal static bool ThrowOnConstruct;

    internal static void Construct()
    {
        Constructions++;
        if (ThrowOnConstruct)
        {
            throw new InvalidOperationException("Test constructor failure.");
        }
    }

    internal static void Reset()
    {
        Constructions = 0;
        Deserializing = 0;
        Deserialized = 0;
        ThrowOnConstruct = false;
    }
}

[TinyhandObject(LockMemberName = "syncObject")]
public partial class ConstructionArrayModel : IConstructionProbe
{
    private readonly object syncObject = new();

    public ConstructionArrayModel() => ConstructionCounters.Construct();

    [Key(0)]
    public int Value { get; set; }

    [IgnoreMember]
    public bool RequireLock { get; set; }

    [TinyhandOnDeserializing]
    private void OnDeserializing()
    {
        Assert.True(!this.RequireLock || Monitor.IsEntered(this.syncObject));
        ConstructionCounters.Deserializing++;
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized() => ConstructionCounters.Deserialized++;
}

[TinyhandObject(LockMemberName = "syncObject")]
public partial class ConstructionMapModel : IConstructionProbe
{
    private readonly object syncObject = new();

    public ConstructionMapModel() => ConstructionCounters.Construct();

    [Key("Value")]
    public int Value { get; set; }

    [IgnoreMember]
    public bool RequireLock { get; set; }

    [TinyhandOnDeserializing]
    private void OnDeserializing()
    {
        Assert.True(!this.RequireLock || Monitor.IsEntered(this.syncObject));
        ConstructionCounters.Deserializing++;
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized() => ConstructionCounters.Deserialized++;
}

[TinyhandObject(UseServiceProvider = true, AddAlternateKey = true, LockMemberName = "syncObject")]
public partial class ConstructionServiceModel : IConstructionProbe
{
    private readonly object syncObject = new();

    public ConstructionServiceModel() => ConstructionCounters.Construct();

    [Key(0)]
    public int Value { get; set; }

    [IgnoreMember]
    public bool RequireLock { get; set; }

    [TinyhandOnDeserializing]
    private void OnDeserializing()
    {
        Assert.True(!this.RequireLock || Monitor.IsEntered(this.syncObject));
        ConstructionCounters.Deserializing++;
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized() => ConstructionCounters.Deserialized++;
}
