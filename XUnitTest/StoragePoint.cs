// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Threading;
using Tinyhand.IO;

namespace Tinyhand.Tests;

[TinyhandObject]
public sealed partial class StoragePoint<TData> : SemaphoreLock, IStructualObject, ITinyhandSerializable<StoragePoint<TData>>, ITinyhandReconstructable<StoragePoint<TData>>, ITinyhandCloneable<StoragePoint<TData>>
{
    #region FieldAndProperty

    public ulong PointId { get; private set; } // Key:0

    IStructualRoot? IStructualObject.StructualRoot { get; set; }

    IStructualObject? IStructualObject.StructualParent { get; set; }

    int IStructualObject.StructualKey { get; set; }

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
}
