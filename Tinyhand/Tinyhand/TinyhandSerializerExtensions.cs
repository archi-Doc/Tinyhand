// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Arc.IO;
using Tinyhand.IO;

namespace Tinyhand;

public static partial class TinyhandSerializerExtensions
{
    /// <summary>
    /// Calculates the XXHash3 hash value for the specified value.
    /// </summary>
    /// <param name="value">The value to calculate the hash for.</param>
    /// <returns>The XXHash3 hash value.</returns>
    public static ulong GetXxHash3(this ITinyhandSerialize value)
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            value.Serialize(ref writer, TinyhandSerializer.DefaultOptions);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return Arc.Crypto.XxHash3.Hash64(span);
        }
        catch
        {
            return 0;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes the specified value using the provided TinyhandWriter and options.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serialization options. If null, default options will be used.</param>
    /// <returns>A byte array containing the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when serialization fails.</exception>
    public static byte[] Serialize(this ITinyhandSerialize value, TinyhandSerializerOptions? options = null)
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            SerializeInternal(value, ref writer, options);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes the specified value using the provided TinyhandWriter and options.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serialization options. If null, default options will be used.</param>
    /// <returns>A <see cref="BytePool.RentMemory"/> containing the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when serialization fails.</exception>
    public static BytePool.RentMemory SerializeToRentMemory(this ITinyhandSerialize value, TinyhandSerializerOptions? options = null)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            SerializeInternal(value, ref writer, options);
            return writer.FlushAndGetRentMemory();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value in signature mode.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="level">The level for serialization (members with this level or lower will be serialized).</param>
    /// <returns>A byte array with the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] SerializeSignature(this ITinyhandSerialize value, int level)
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = level;
        try
        {
            SerializeInternal(value, ref writer, TinyhandSerializerOptions.Signature);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value in signature mode.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="level">The level for serialization (members with this level or lower will be serialized).</param>
    /// <returns>A byte array with the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static BytePool.RentMemory SerializeSignatureToRentMemory(this ITinyhandSerialize value, int level)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = level;
        try
        {
            SerializeInternal(value, ref writer, TinyhandSerializerOptions.Signature);
            return writer.FlushAndGetRentMemory();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static bool TryDeserialize(this ITinyhandSerialize value, ref TinyhandReader reader, TinyhandSerializerOptions? options = null)
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        try
        {
            if (options.HasLz4CompressFlag)
            {
                var byteSequence = new ByteSequence();
                try
                {
                    if (TinyhandSerializer.TryDecompress(ref reader, byteSequence))
                    {
                        var r = reader.Clone(byteSequence.ToReadOnlySpan());
                        value.Deserialize(ref r, options);
                    }
                    else
                    {
                        value.Deserialize(ref reader, options);
                    }
                }
                finally
                {
                    byteSequence.Dispose();
                }
            }
            else
            {
                value.Deserialize(ref reader, options);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryDeserialize(this ITinyhandSerialize value, ReadOnlySpan<byte> data, TinyhandSerializerOptions? options = null)
    {
        var reader = new TinyhandReader(data);
        return value.TryDeserialize(ref reader, options);
    }

    /// <summary>
    /// Gets the type identifier (FarmHash.Hash64(Type.FullName)) for the specified value.
    /// </summary>
    /// <param name="value">The value to get the type identifier for.</param>
    /// <returns>The type identifier.</returns>
    public static ulong GetTypeIdentifier(this ITinyhandSerialize value)
        => value.GetTypeIdentifier(); // GetTypeIdentifierCode

    /// <summary>
    /// Serializes the specified value using the provided TinyhandWriter and options.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="writer">The TinyhandWriter to use for serialization.</param>
    /// <param name="options">The serialization options. If null, default options will be used.</param>
    /// <exception cref="TinyhandException">Thrown when serialization fails.</exception>
    internal static void SerializeInternal(this ITinyhandSerialize value, ref TinyhandWriter writer, TinyhandSerializerOptions? options = null)
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        try
        {
            if (options.HasLz4CompressFlag)
            {
                var w = writer.Clone(TinyhandSerializer.GetThreadStaticBuffer2());
                try
                {
                    value.Serialize(ref w, options);
                    TinyhandSerializer.ToLZ4BinaryCore(w.FlushAndGetReadOnlySequence(), ref writer);
                }
                finally
                {
                    w.Dispose();
                }
            }
            else
            {
                value.Serialize(ref writer, options);
            }
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to serialize the value.", ex);
        }
    }
}
