// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Arc;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class StringValueSecurityTest
{
    private static readonly TinyhandSerializerOptions Untrusted = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };

    [Theory]
    [InlineData(new byte[] { 0xff })]
    [InlineData(new byte[] { 0xc0, 0xaf })]
    [InlineData(new byte[] { 0xed, 0xa0, 0x80 })]
    [InlineData(new byte[] { 0xf4, 0x90, 0x80, 0x80 })]
    [InlineData(new byte[] { 0xe2, 0x82 })]
    public void InvalidUtf8IsRejectedInStringsIdentifiersAndSkippedValues(byte[] payload)
    {
        for (var encoding = 0; encoding < 7; encoding++)
        {
            var data = Encode(payload, encoding);
            Assert.Throws<TinyhandException>(() => Skip(data, TinyhandSecurity.UntrustedData));
            if (encoding <= 4)
            {
                Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<string>(data, Untrusted));
                Assert.Equal(Encoding.UTF8.GetString(payload), TinyhandSerializer.Deserialize<string>(data, TinyhandSerializerOptions.Standard));
                Assert.Throws<TinyhandException>(() => ReadIdentifier(data, false));
                Assert.Throws<TinyhandException>(() => ReadIdentifier(data, true));
            }

            if (encoding < 4)
            {
                Assert.Throws<TinyhandException>(() => ReadSpan(data, false));
                Assert.Throws<TinyhandException>(() => ReadSpan(data, true));
            }
            else
            {
                Assert.Throws<TinyhandException>(() => ReadExtension(data));
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("日本語 😀 \ufffd")]
    public void ValidUnicodeAndReplacementCharacterRemainValid(string text)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        for (var encoding = 0; encoding < 7; encoding++)
        {
            var data = Encode(payload, encoding);
            Skip(data, TinyhandSecurity.UntrustedData);
            if (encoding <= 4)
            {
                Assert.Equal(text, TinyhandSerializer.Deserialize<string>(data, Untrusted));
            }
        }
    }

    [Fact]
    public void BinaryPayloadsAreNotInterpretedAsUtf8()
    {
        byte[] data = [MessagePackCode.Bin8, 2, 0xff, 0xc0];
        Assert.Equal(new byte[] { 0xff, 0xc0 }, TinyhandSerializer.Deserialize<byte[]>(data, Untrusted));
        var reader = new TinyhandReader(data, TinyhandSecurity.UntrustedData);
        Assert.True(reader.TryReadBytes(out var payload));
        Assert.Equal(new byte[] { 0xff, 0xc0 }, payload.ToArray());
        Skip(data, TinyhandSecurity.UntrustedData);
    }

    [Fact]
    public void MalformedDictionaryKeyCannotAliasTheReplacementCharacter()
    {
        byte[] data = [0x81, 0xa1, 0xff, 42];
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<Dictionary<string, int>>(data, Untrusted));
        Assert.Equal(42, TinyhandSerializer.Deserialize<Dictionary<string, int>>(data, TinyhandSerializerOptions.Standard)!["\ufffd"]);
    }

    [Fact]
    public void UnknownGeneratedFieldsStillValidateStrings()
    {
        byte[] data = [0x91, 0xa1, 0xff];
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<LimitsEmpty>(data, Untrusted));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ValidationSurvivesCopiesAndUnrestrictedPolicies(int copyKind)
    {
        Assert.Throws<TinyhandException>(() => ReadThroughCopy(copyKind));
    }

    [Fact]
    public void DecompressedReaderRetainsStringValidation()
    {
        byte[] data = [0x92, 0xc7, 1, MessagePackExtensionCodes.Lz4BlockArray, 2, 0xc4, 3, 0x20, 0xa1, 0xff];
        var options = TinyhandSerializerOptions.Lz4 with { Security = TinyhandSecurity.UntrustedData };
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<string>(data, options));
        Assert.Equal("\ufffd", TinyhandSerializer.Deserialize<string>(data, TinyhandSerializerOptions.Lz4));
    }

    [Fact]
    public void StringValidationCanBeExplicitlyDisabled()
    {
        var options = Untrusted with { Security = TinyhandSecurity.UntrustedData.WithValidateStringValues(false) };
        Assert.Equal("\ufffd", TinyhandSerializer.Deserialize<string>(new byte[] { 0xa1, 0xff }, options));
        Assert.True(TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(4).ValidateStringValues);
        Assert.False(TinyhandSecurity.TrustedData.ValidateStringValues);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("ok trailing")]
    public void FailedOrPartialStringConversionDoesNotOverwriteExistingValue(string text)
    {
        var data = TinyhandSerializer.Serialize(text);
        foreach (var mode in new[] { 0, 1, 2 })
        {
            var original = new SecurityStringValue { Value = 7 };
            SecurityStringValue? value = original;
            Assert.Throws<TinyhandException>(() => ReadConvertible(data, ref value, mode, Untrusted));
            Assert.Same(original, value);

            ReadConvertible(data, ref value, mode, TinyhandSerializerOptions.Standard);
            Assert.Equal(text == "bad" ? -1 : 1, value!.Value);
        }
    }

    [Fact]
    public void CompleteStringConversionSucceeds()
    {
        var data = TinyhandSerializer.Serialize("ok");
        foreach (var mode in new[] { 0, 1, 2 })
        {
            SecurityStringValue? value = null;
            ReadConvertible(data, ref value, mode, Untrusted);
            Assert.Equal(1, value!.Value);
        }
    }

    private static byte[] Encode(byte[] payload, int encoding)
    {
        var headerLength = encoding switch { 0 => 1, 1 => 2, 2 or 5 => 3, 3 => 5, 4 => 6, _ => 4 };
        var data = new byte[headerLength + payload.Length];
        switch (encoding)
        {
            case 0: data[0] = (byte)(MessagePackCode.MinFixStr | payload.Length); break;
            case 1: data[0] = MessagePackCode.Str8; data[1] = (byte)payload.Length; break;
            case 2: data[0] = MessagePackCode.Str16; BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(1), (ushort)payload.Length); break;
            case 3: data[0] = MessagePackCode.Str32; BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(1), payload.Length); break;
            case 4: data[0] = MessagePackCode.Ext32; BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(1), payload.Length); data[5] = MessagePackExtensionCodes.Identifier; break;
            case 5: data[0] = MessagePackCode.Ext8; data[1] = (byte)payload.Length; data[2] = MessagePackExtensionCodes.Identifier; break;
            default: data[0] = MessagePackCode.Ext16; BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(1), (ushort)payload.Length); data[3] = MessagePackExtensionCodes.Identifier; break;
        }

        payload.CopyTo(data, headerLength);
        return data;
    }

    private static void Skip(byte[] data, TinyhandSecurity security)
    {
        var reader = new TinyhandReader(data, security);
        reader.Skip();
        Assert.True(reader.End);
    }

    private static void ReadSpan(byte[] data, bool tryRead)
    {
        var reader = new TinyhandReader(data, TinyhandSecurity.UntrustedData);
        if (tryRead)
        {
            reader.TryReadStringSpan(out _);
        }
        else
        {
            reader.ReadStringSpan();
        }
    }

    private static void ReadExtension(byte[] data)
    {
        var reader = new TinyhandReader(data, TinyhandSecurity.UntrustedData);
        reader.ReadExtensionFormatHeader();
    }

    private static void ReadIdentifier(byte[] data, bool utf16)
    {
        var reader = new TinyhandReader(data, TinyhandSecurity.UntrustedData);
        if (utf16)
        {
            reader.ReadIdentifierUtf16();
        }
        else
        {
            reader.ReadIdentifierUtf8();
        }
    }

    private static void ReadThroughCopy(int copyKind)
    {
        byte[] data = [0xa1, 0xff];
        var reader = new TinyhandReader(data, TinyhandSecurity.UntrustedData.WithLimits(TinyhandDeserializationLimits.Unrestricted));
        var copy = copyKind switch { 0 => reader, 1 => reader.Fork(), _ => reader.CreateSubReader(data) };
        copy.ApplySecurity(TinyhandSecurity.TrustedData.WithLimits(TinyhandDeserializationLimits.Unrestricted));
        copy.ReadString();
    }

    private static void ReadConvertible(byte[] data, ref SecurityStringValue? value, int mode, TinyhandSerializerOptions options)
    {
        var reader = new TinyhandReader(data, options.Security);
        if (mode == 0)
        {
            reader.ReadStringConvertible(ref value);
        }
        else if (mode == 1)
        {
            TinyhandSerializer.ReadStringConvertibleOrDeserializeObject(ref reader, ref value, options);
        }
        else
        {
            TinyhandSerializer.ReadStringConvertibleOrDeserializeAndReconstructObject(ref reader, ref value!, options);
        }
    }
}

[TinyhandObject]
public partial class SecurityStringValue : IStringConvertible<SecurityStringValue>
{
    [Key(0)]
    public int Value { get; set; }

    public static int MaxStringLength => -1;

    public static bool TryParse(ReadOnlySpan<char> source, out SecurityStringValue? instance, out int read, IConversionOptions? conversionOptions = default)
    {
        var success = source.StartsWith("ok", StringComparison.Ordinal);
        read = success ? 2 : 0;
        instance = new SecurityStringValue { Value = success ? 1 : -1 };
        return success;
    }

    public int GetStringLength() => 2;

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default)
    {
        var success = "ok".AsSpan().TryCopyTo(destination);
        written = success ? 2 : 0;
        return success;
    }
}
