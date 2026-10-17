// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Arc.IO;
using Tinyhand.IO;

namespace Tinyhand;

public static partial class TinyhandSerializer
{
    /// <summary>Deserializes exactly one complete value, rejecting trailing bytes both outside and inside an LZ4 envelope.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="buffer">One complete application frame.</param>
    /// <param name="options">The deserialization policy; defaults to <see cref="DefaultOptions"/>.</param>
    /// <returns>The deserialized value.</returns>
    /// <remarks>Structural validation precedes object construction. Schema validation and application callbacks are not transactional.</remarks>
    public static T? DeserializeExact<T>(ReadOnlySpan<byte> buffer, TinyhandSerializerOptions? options = null)
    {
        options ??= DefaultOptions;
        var reader = new TinyhandReader(buffer);
        try
        {
            reader.ApplySecurity(options.Security);
            if (options.HasLz4CompressFlag)
            {
                var sequence = new ByteSequence();
                try
                {
                    if (TryDecompress(ref reader, sequence))
                    {
                        EnsureEnd(ref reader);
                        var decompressed = reader.CreateSubReader(sequence.ToReadOnlySpan());
                        return DeserializeSingleValue<T>(ref decompressed, options);
                    }
                }
                finally
                {
                    sequence.Dispose();
                }
            }

            return DeserializeSingleValue<T>(ref reader, options);
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to deserialize exactly one {typeof(T).FullName} value.", ex);
        }
    }

    /// <summary>Attempts to deserialize exactly one complete, non-null value.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="buffer">One complete application frame.</param>
    /// <param name="value">The value on success, or the default value on failure.</param>
    /// <param name="options">The deserialization policy.</param>
    /// <returns>Whether a complete non-null value was deserialized.</returns>
    public static bool TryDeserializeExact<T>(ReadOnlySpan<byte> buffer, [MaybeNullWhen(false)] out T value, TinyhandSerializerOptions? options = null)
    {
        try
        {
            value = DeserializeExact<T>(buffer, options);
            return value is not null;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    internal static void ValidateSingleValue(ref TinyhandReader reader)
    {
        // Validation must not consume the shared allocation/payload budgets a second time.
        var validation = reader.ForkForValidation();
        validation.Skip();
        EnsureEnd(ref validation);
    }

    internal static void EnsureEnd(ref TinyhandReader reader)
    {
        if (!reader.End)
        {
            throw new TinyhandException("Trailing data follows the deserialized value.");
        }
    }

    private static T? DeserializeSingleValue<T>(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        ValidateSingleValue(ref reader);
        var value = options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
        EnsureEnd(ref reader);
        return value;
    }
}
