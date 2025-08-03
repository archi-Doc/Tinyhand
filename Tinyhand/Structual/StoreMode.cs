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
    /// If the data is locked, it will be persisted and released once the lock is released.
    /// </summary>
    Release,

    /*
    /// <summary>
    /// The data is persisted, and the process waits until unloading is complete.
    /// </summary>
    AlwaysUnload,

    /// <summary>
    /// Similar to <see cref="AlwaysUnload" />, this waits until the unload process is complete.<br/>
    /// Additionally, it sets the target StoragePoint to the unloaded state.<br/>
    /// This is intended for use during service or application shutdown.
    /// </summary>
    UnloadAll,*/
}
