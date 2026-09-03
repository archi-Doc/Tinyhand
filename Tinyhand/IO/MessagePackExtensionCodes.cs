// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

/// <summary>
/// Defines the MessagePack extension codes used by Tinyhand.
/// </summary>
public static class MessagePackExtensionCodes
{
    /// <summary>
    /// The extension code for an eight-byte DateTime binary value.
    /// </summary>
    public const byte DateTime = 99;

    /// <summary>
    /// The LZ4 array block compression extension.
    /// </summary>
    public const byte Lz4BlockArray = 98;

    /// <summary>
    /// Identifier(UTF8/16) extension.
    /// </summary>
    public const byte Identifier = 97;

    /// <summary>
    /// Int128.
    /// </summary>
    public const byte Int128 = 96;

    /// <summary>
    /// UInt128.
    /// </summary>
    public const byte UInt128 = 95;
}
