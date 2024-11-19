// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Arc.IO;
using Tinyhand.IO;

namespace Tinyhand;

public static partial class TinyhandSerializer
{
    /// <summary>
    /// Serializes the specified value using the provided TinyhandWriter and options.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="writer">The TinyhandWriter to use for serialization.</param>
    /// <param name="options">The serialization options. If null, default options will be used.</param>
    /// <exception cref="TinyhandException">Thrown when serialization fails.</exception>
    public static void SerializeInterface(this ITinyhandSerialize value, ref TinyhandWriter writer, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        try
        {
            if (options.HasLz4CompressFlag)
            {
                if (initialBuffer2 == null)
                {
                    initialBuffer2 = new byte[InitialBufferSize];
                }

                var w = writer.Clone(initialBuffer2);
                try
                {
                    value.Serialize(ref w, options);
                    ToLZ4BinaryCore(w.FlushAndGetReadOnlySequence(), ref writer);
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
    public static byte[] SerializeInterface(this ITinyhandSerialize value, TinyhandSerializerOptions? options = null)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        var writer = new TinyhandWriter(initialBuffer);
        try
        {
            value.SerializeInterface(ref writer, options);
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
    public static BytePool.RentMemory SerializeInterfaceToRentMemory(this ITinyhandSerialize value, TinyhandSerializerOptions? options = null)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            value.SerializeInterface(ref writer, options);
            return writer.FlushAndGetRentMemory();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static bool TryDeserializeInterface(this ITinyhandSerialize value, ref TinyhandReader reader, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        try
        {
            if (options.HasLz4CompressFlag)
            {
                var byteSequence = new ByteSequence();
                try
                {
                    if (TryDecompress(ref reader, byteSequence))
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

    public static bool TryDeserializeInterface(this ITinyhandSerialize value, ReadOnlySpan<byte> data, TinyhandSerializerOptions? options = null)
    {
        var reader = new TinyhandReader(data);
        return value.TryDeserializeInterface(ref reader, options);
    }
}
