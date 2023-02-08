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
    internal const sbyte Identifier = 99;
}
