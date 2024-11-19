// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Arc.IO;
using Tinyhand.IO;

namespace Tinyhand;

public static partial class TinyhandSerializerExtensions
{
    /// <summary>
    /// Serializes the specified value using the provided TinyhandWriter and options.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="writer">The TinyhandWriter to use for serialization.</param>
    /// <param name="options">The serialization options. If null, default options will be used.</param>
    /// <exception cref="TinyhandException">Thrown when serialization fails.</exception>
    public static void Serialize(this ITinyhandSerialize value, ref TinyhandWriter writer, TinyhandSerializerOptions? options = null)
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        try
        {
            if (options.HasLz4CompressFlag)
            {
                if (TinyhandSerializer.InitialBuffer2 == null)
                {
                    TinyhandSerializer.InitialBuffer2 = new byte[TinyhandSerializer.InitialBufferSize];
                }

                var w = writer.Clone(TinyhandSerializer.InitialBuffer2);
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

    /// <summary>
    /// Serializes the specified value using the provided TinyhandWriter and options.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serialization options. If null, default options will be used.</param>
    /// <returns>A byte array containing the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when serialization fails.</exception>
    public static byte[] Serialize(this ITinyhandSerialize value, TinyhandSerializerOptions? options = null)
    {
        if (TinyhandSerializer.InitialBuffer == null)
        {
            TinyhandSerializer.InitialBuffer = new byte[TinyhandSerializer.InitialBufferSize];
        }

        var writer = new TinyhandWriter(TinyhandSerializer.InitialBuffer);
        try
        {
            Serialize(value, ref writer, options);
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
            Serialize(value, ref writer, options);
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
}
