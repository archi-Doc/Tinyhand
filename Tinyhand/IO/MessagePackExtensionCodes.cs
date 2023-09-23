// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

/// <summary>
/// The extension type codes that this library defines for just this library.
/// </summary>
internal static class MessagePackExtensionCodes
{
    /// <summary>
    /// The LZ4 array block compression extension.
    /// </summary>
    internal const sbyte Lz4BlockArray = 98;

    /// <summary>
    /// Identifier(UTF8/16) extension.
    /// </summary>
    internal const sbyte Identifier = 97;

    /// <summary>
    /// Int128.
    /// </summary>
    internal const sbyte Int128 = 96;

    /// <summary>
    /// UInt128.
    /// </summary>
    internal const sbyte UInt128 = 95;
}
