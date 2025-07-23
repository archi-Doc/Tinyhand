// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Threading;
using Tinyhand;
using Tinyhand.IO;

namespace Tinyhand.Tests;

[TinyhandObject]
public sealed partial class StoragePoint<TData> : SemaphoreLock, IStructualObject, ITinyhandSerializable<StoragePoint<TData>>, ITinyhandReconstructable<StoragePoint<TData>>, ITinyhandCloneable<StoragePoint<TData>>
{//Configuration of external types...
    public const int MaxHistories = 3; // 4

    private const uint InvalidBit = 1u << 31;
    private const uint UnloadingBit = 1u << 30;
    private const uint UnloadedBit = 1u << 29;
    private const uint UnloadingAndUnloadedBit = UnloadingBit | UnloadedBit;
    private const uint StateMask = 0xFFFF0000;
    private const uint NegativeStateMask = 0xFF000000;
    private const uint LockCountMask = 0x0000FFFF;

    #region FieldAndProperty

    public ulong PointId { get; private set; } // Key:0

    IStructualRoot? IStructualObject.StructualRoot { get; set; }

    IStructualObject? IStructualObject.StructualParent { get; set; }

    int IStructualObject.StructualKey { get; set; }

    // 31bit:Invalid storage, 30bit:Unloading, 29bit:Unload, 23-0bit:Lock count.
    private uint state; // SemaphoreLock

    public bool IsActive => (this.state & NegativeStateMask) == 0;

    public bool IsInvalid => (this.state & InvalidBit) != 0;

    public bool IsUnloadingOrUnloaded => (this.state & UnloadingAndUnloadedBit) != 0;

    public bool CanUnload => this.LockCount == 0;

    private uint LockCount => this.state & LockCountMask;

    #endregion

    #region Tinyhand

    static void ITinyhandSerializable<StoragePoint<TData>>.Serialize(ref TinyhandWriter writer, scoped ref StoragePoint<TData>? v, TinyhandSerializerOptions options)
    {
        if (v == null)
        {
            writer.WriteNil();
            return;
        }
    }

    static void ITinyhandSerializable<StoragePoint<TData>>.Deserialize(ref TinyhandReader reader, scoped ref StoragePoint<TData>? v, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        v ??= new StoragePoint<TData>();
        if (!options.IsCustomMode)
        {
            v.PointId = reader.ReadUInt64();
            return;
        }
    }

    static void ITinyhandReconstructable<StoragePoint<TData>>.Reconstruct([NotNull] scoped ref StoragePoint<TData>? v, TinyhandSerializerOptions options)
    {
        v ??= new StoragePoint<TData>();
    }

    static StoragePoint<TData>? ITinyhandCloneable<StoragePoint<TData>>.Clone(scoped ref StoragePoint<TData>? v, TinyhandSerializerOptions options)
    {
        if (v == null)
        {
            return null;
        }

        var value = new StoragePoint<TData>();
        value.PointId = v.PointId;
        return value;
    }

    #endregion

    public StoragePoint()
    {
    }

    public StoragePoint(bool invalidStorage = false)
    {
        if (invalidStorage)
        {
            this.state |= InvalidBit;
        }
    }
}
