// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

public enum StoreMode
{
    /// <summary>
    /// The data is persisted, but the memory state is not changed.
    /// </summary>
    StoreOnly,

    /// <summary>
    /// Attempts to persist the data and release resources.<br/>
    /// If the data is locked, the method exits without performing any action.
    /// </summary>
    TryRelease,

    /// <summary>
    /// Persist the data and release resources.
    /// If the data is locked, wait until the lock is released.
    /// </summary>
    ForceRelease,
}
