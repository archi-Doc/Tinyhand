// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;

namespace Tinyhand;

/// <summary>
/// Represents the MessagePack nil value.
/// </summary>
public struct Nil : IEquatable<Nil>
{
    /// <summary>
    /// The default nil value.
    /// </summary>
    public static readonly Nil Default = default(Nil);

    /// <summary>
    /// Checks whether an object represents nil.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>Whether the object is a nil value.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Nil;
    }

    /// <summary>
    /// Compares two nil values.
    /// </summary>
    /// <param name="other">The value to compare.</param>
    /// <returns>Always true.</returns>
    public bool Equals(Nil other)
    {
        return true;
    }

    /// <summary>
    /// Gets the hash code for nil.
    /// </summary>
    /// <returns>Zero.</returns>
    public override int GetHashCode()
    {
        return 0;
    }

    /// <summary>
    /// Gets the text representation of nil.
    /// </summary>
    /// <returns>The string ().</returns>
    public override string ToString()
    {
        return "()";
    }
}

/// <summary>
/// Describes a MessagePack extension's type and payload length.
/// </summary>
public struct ExtensionHeader : IEquatable<ExtensionHeader>
{
    /// <summary>
    /// Gets the extension type code.
    /// </summary>
    public byte TypeCode { get; private set; }

    /// <summary>
    /// Gets the extension payload length in bytes.
    /// </summary>
    public uint Length { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionHeader"/> struct.
    /// </summary>
    /// <param name="typeCode">The extension type code.</param>
    /// <param name="length">The nonnegative payload length in bytes.</param>
    public ExtensionHeader(byte typeCode, uint length)
    {
        this.TypeCode = typeCode;
        this.Length = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionHeader"/> struct.
    /// </summary>
    /// <param name="typeCode">The extension type code.</param>
    /// <param name="length">The nonnegative payload length in bytes.</param>
    public ExtensionHeader(byte typeCode, int length)
    {
        this.TypeCode = typeCode;
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        this.Length = (uint)length;
    }

    /// <summary>
    /// Compares the extension type and payload length.
    /// </summary>
    /// <param name="other">The header to compare.</param>
    /// <returns>Whether both fields match.</returns>
    public bool Equals(ExtensionHeader other) => this.TypeCode == other.TypeCode && this.Length == other.Length;
}

/// <summary>
/// Contains an extension type code and its encoded payload.
/// </summary>
public struct ExtensionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionResult"/> struct.
    /// </summary>
    /// <param name="typeCode">The extension type code.</param>
    /// <param name="data">The payload bytes.</param>
    public ExtensionResult(byte typeCode, Memory<byte> data)
    {
        this.TypeCode = typeCode;
        this.Data = new ReadOnlySequence<byte>(data);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionResult"/> struct.
    /// </summary>
    /// <param name="typeCode">The extension type code.</param>
    /// <param name="data">The payload bytes.</param>
    public ExtensionResult(byte typeCode, ReadOnlySequence<byte> data)
    {
        this.TypeCode = typeCode;
        this.Data = data;
    }

    /// <summary>
    /// Gets the extension type code.
    /// </summary>
    public byte TypeCode { get; private set; }

    /// <summary>
    /// Gets the extension payload bytes.
    /// </summary>
    public ReadOnlySequence<byte> Data { get; private set; }

    /// <summary>
    /// Gets the header for this payload.
    /// </summary>
    public ExtensionHeader Header => new ExtensionHeader(this.TypeCode, checked((uint)this.Data.Length));
}

internal static class DateTimeConstants
{
    internal const long BclSecondsAtUnixEpoch = 62135596800;
    internal const int NanosecondsPerTick = 100;
    internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

/// <summary>
/// Identifies the value represented by a MessagePack format code.
/// </summary>
public enum MessagePackType : byte
{
    /// <summary>
    /// An unrecognized or reserved format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A signed or unsigned integer.
    /// </summary>
    Integer = 1,

    /// <summary>
    /// A nil value.
    /// </summary>
    Nil = 2,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// A floating-point value.
    /// </summary>
    Float = 4,

    /// <summary>
    /// A UTF-8 string.
    /// </summary>
    String = 5,

    /// <summary>
    /// A binary payload.
    /// </summary>
    Binary = 6,

    /// <summary>
    /// An ordered collection of values.
    /// </summary>
    Array = 7,

    /// <summary>
    /// A collection of key-value pairs.
    /// </summary>
    Map = 8,

    /// <summary>
    /// An application-defined extension.
    /// </summary>
    Extension = 9,
}

/// <summary>
/// The core type codes as defined by msgpack.
/// </summary>
/// <seealso href="https://github.com/msgpack/msgpack/blob/master/spec.md#overview" />
public static class MessagePackCode
{
    /// <summary>
    /// The first positive fixint code.
    /// </summary>
    public const byte MinFixInt = 0x00; // 0

    /// <summary>
    /// The last positive fixint code.
    /// </summary>
    public const byte MaxFixInt = 0x7f; // 127

    /// <summary>
    /// The first fixed map code.
    /// </summary>
    public const byte MinFixMap = 0x80; // 128

    /// <summary>
    /// The last fixed map code.
    /// </summary>
    public const byte MaxFixMap = 0x8f; // 143

    /// <summary>
    /// The first fixed array code.
    /// </summary>
    public const byte MinFixArray = 0x90; // 144

    /// <summary>
    /// The last fixed array code.
    /// </summary>
    public const byte MaxFixArray = 0x9f; // 159

    /// <summary>
    /// The first fixed string code.
    /// </summary>
    public const byte MinFixStr = 0xa0; // 160

    /// <summary>
    /// The last fixed string code.
    /// </summary>
    public const byte MaxFixStr = 0xbf; // 191

    /// <summary>
    /// The nil code.
    /// </summary>
    public const byte Nil = 0xc0;

    /// <summary>
    /// The reserved, invalid format code.
    /// </summary>
    public const byte NeverUsed = 0xc1;

    /// <summary>
    /// The false code.
    /// </summary>
    public const byte False = 0xc2;

    /// <summary>
    /// The true code.
    /// </summary>
    public const byte True = 0xc3;

    /// <summary>
    /// The binary code with a 8-bit length.
    /// </summary>
    public const byte Bin8 = 0xc4;

    /// <summary>
    /// The binary code with a 16-bit length.
    /// </summary>
    public const byte Bin16 = 0xc5;

    /// <summary>
    /// The binary code with a 32-bit length.
    /// </summary>
    public const byte Bin32 = 0xc6;

    /// <summary>
    /// The extension code with a 8-bit length.
    /// </summary>
    public const byte Ext8 = 0xc7;

    /// <summary>
    /// The extension code with a 16-bit length.
    /// </summary>
    public const byte Ext16 = 0xc8;

    /// <summary>
    /// The extension code with a 32-bit length.
    /// </summary>
    public const byte Ext32 = 0xc9;

    /// <summary>
    /// The 32-bit floating-point code.
    /// </summary>
    public const byte Float32 = 0xca;

    /// <summary>
    /// The 64-bit floating-point code.
    /// </summary>
    public const byte Float64 = 0xcb;

    /// <summary>
    /// The unsigned 8-bit integer code.
    /// </summary>
    public const byte UInt8 = 0xcc;

    /// <summary>
    /// The unsigned 16-bit integer code.
    /// </summary>
    public const byte UInt16 = 0xcd;

    /// <summary>
    /// The unsigned 32-bit integer code.
    /// </summary>
    public const byte UInt32 = 0xce;

    /// <summary>
    /// The unsigned 64-bit integer code.
    /// </summary>
    public const byte UInt64 = 0xcf;

    /// <summary>
    /// The signed 8-bit integer code.
    /// </summary>
    public const byte Int8 = 0xd0;

    /// <summary>
    /// The signed 16-bit integer code.
    /// </summary>
    public const byte Int16 = 0xd1;

    /// <summary>
    /// The signed 32-bit integer code.
    /// </summary>
    public const byte Int32 = 0xd2;

    /// <summary>
    /// The signed 64-bit integer code.
    /// </summary>
    public const byte Int64 = 0xd3;

    /// <summary>
    /// The extension code for a 1-byte payload.
    /// </summary>
    public const byte FixExt1 = 0xd4;

    /// <summary>
    /// The extension code for a 2-byte payload.
    /// </summary>
    public const byte FixExt2 = 0xd5;

    /// <summary>
    /// The extension code for a 4-byte payload.
    /// </summary>
    public const byte FixExt4 = 0xd6;

    /// <summary>
    /// The extension code for a 8-byte payload.
    /// </summary>
    public const byte FixExt8 = 0xd7;

    /// <summary>
    /// The extension code for a 16-byte payload.
    /// </summary>
    public const byte FixExt16 = 0xd8;

    /// <summary>
    /// The string code with a 8-bit length.
    /// </summary>
    public const byte Str8 = 0xd9;

    /// <summary>
    /// The string code with a 16-bit length.
    /// </summary>
    public const byte Str16 = 0xda;

    /// <summary>
    /// The string code with a 32-bit length.
    /// </summary>
    public const byte Str32 = 0xdb;

    /// <summary>
    /// The array code with a 16-bit count.
    /// </summary>
    public const byte Array16 = 0xdc;

    /// <summary>
    /// The array code with a 32-bit count.
    /// </summary>
    public const byte Array32 = 0xdd;

    /// <summary>
    /// The map code with a 16-bit count.
    /// </summary>
    public const byte Map16 = 0xde;

    /// <summary>
    /// The map code with a 32-bit count.
    /// </summary>
    public const byte Map32 = 0xdf;

    /// <summary>
    /// The first negative fixint code.
    /// </summary>
    public const byte MinNegativeFixInt = 0xe0; // 224

    /// <summary>
    /// The last negative fixint code.
    /// </summary>
    public const byte MaxNegativeFixInt = 0xff; // 255

    // Stored in the assembly's read-only data; no managed array or type initializer is needed.
    private static ReadOnlySpan<byte> TypeLookupTable =>
    [
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x00
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x10
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x20
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x30
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x40
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x50
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x60
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x70
        8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, // 0x80
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, // 0x90
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, // 0xa0
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, // 0xb0
        2, 0, 3, 3, 6, 6, 6, 9, 9, 9, 4, 4, 1, 1, 1, 1, // 0xc0
        1, 1, 1, 1, 9, 9, 9, 9, 9, 5, 5, 5, 7, 7, 8, 8, // 0xd0
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0xe0
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0xf0
    ];

    /// <summary>
    /// Gets the value type associated with a MessagePack code.
    /// </summary>
    /// <param name="code">The format code.</param>
    /// <returns>The value type.</returns>
    public static MessagePackType ToMessagePackType(byte code) => (MessagePackType)TypeLookupTable[code];

    /// <summary>
    /// Gets the display name of a MessagePack format.
    /// </summary>
    /// <param name="code">The format code.</param>
    /// <returns>The format name.</returns>
    public static string ToFormatName(byte code) => code switch
    {
        <= MaxFixInt => "positive fixint",
        <= MaxFixMap => "fixmap",
        <= MaxFixArray => "fixarray",
        <= MaxFixStr => "fixstr",
        Nil => "nil",
        NeverUsed => "(never used)",
        False => "false",
        True => "true",
        Bin8 => "bin 8",
        Bin16 => "bin 16",
        Bin32 => "bin 32",
        Ext8 => "ext 8",
        Ext16 => "ext 16",
        Ext32 => "ext 32",
        Float32 => "float 32",
        Float64 => "float 64",
        UInt8 => "uint 8",
        UInt16 => "uint 16",
        UInt32 => "uint 32",
        UInt64 => "uint 64",
        Int8 => "int 8",
        Int16 => "int 16",
        Int32 => "int 32",
        Int64 => "int 64",
        FixExt1 => "fixext 1",
        FixExt2 => "fixext 2",
        FixExt4 => "fixext 4",
        FixExt8 => "fixext 8",
        FixExt16 => "fixext 16",
        Str8 => "str 8",
        Str16 => "str 16",
        Str32 => "str 32",
        Array16 => "array 16",
        Array32 => "array 32",
        Map16 => "map 16",
        Map32 => "map 32",
        _ => "negative fixint",
    };

    /// <summary>
    /// Checks whether a given messagepack code represents an integer that might include a sign (i.e. might be a negative number).
    /// </summary>
    /// <param name="code">The messagepack code.</param>
    /// <returns>A boolean value.</returns>
    internal static bool IsSignedInteger(byte code)
        => (uint)(code - Int8) <= Int64 - Int8 | code >= MinNegativeFixInt;
}

/// <summary>
/// Defines the value limits for fixed MessagePack encodings.
/// </summary>
public static class MessagePackRange
{
    /// <summary>
    /// The smallest negative fixint value.
    /// </summary>
    public const int MinFixNegativeInt = -32;

    /// <summary>
    /// The largest negative fixint value.
    /// </summary>
    public const int MaxFixNegativeInt = -1;

    /// <summary>
    /// The largest positive fixint value.
    /// </summary>
    public const int MaxFixPositiveInt = 127;

    /// <summary>
    /// The minimum fixed string byte length.
    /// </summary>
    public const int MinFixStringLength = 0;

    /// <summary>
    /// The maximum fixed string byte length.
    /// </summary>
    public const int MaxFixStringLength = 31;

    /// <summary>
    /// The maximum fixed map pair count.
    /// </summary>
    public const int MaxFixMapCount = 15;

    /// <summary>
    /// The maximum fixed array element count.
    /// </summary>
    public const int MaxFixArrayCount = 15;
}
