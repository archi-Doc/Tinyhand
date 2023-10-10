// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

public enum UnloadMode
{
    /// <summary>
    /// The data is persisted. The data in memory remains unchanged.
    /// </summary>
    NoUnload,

    /// <summary>
    /// Attempts to unload the data. If unloadable, the data is persisted and then unloaded.
    /// </summary>
    TryUnload,

    /// <summary>
    /// The data is persisted and forcibly unloaded.
    /// </summary>
    ForceUnload,
}
