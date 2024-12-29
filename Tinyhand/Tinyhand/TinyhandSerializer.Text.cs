// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Threading;
using Arc.Collections;
using Arc.IO;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Tinyhand;

public static partial class TinyhandSerializer
{
    /// <summary>
    /// Serializes a given value with the specified buffer writer.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] SerializeObjectToUtf8<T>(T value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? DefaultOptions;
        var binary = SerializeObject(value, options);
        bool omitTopLevelBracket; // = OmitTopLevelBracket<T>(options);
        if (options.Compose == TinyhandComposeOption.Strict)
        {
            omitTopLevelBracket = false;
        }
        else
        {
            omitTopLevelBracket = OmitTopLevelBracketCache<T>.CanOmit;
        }

        var writer = new TinyhandRawWriter(GetThreadStaticBuffer());
        try
        {
            TinyhandTreeConverter.FromBinaryToUtf8(binary, ref writer, options, omitTopLevelBracket);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value with the specified buffer writer.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static BytePool.RentMemory SerializeObjectToUtf8RentMemory<T>(T value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? DefaultOptions;
        var rentMemory = SerializeObjectToRentMemory(value, options);
        bool omitTopLevelBracket; // = OmitTopLevelBracket<T>(options);
        if (options.Compose == TinyhandComposeOption.Strict)
        {
            omitTopLevelBracket = false;
        }
        else
        {
            omitTopLevelBracket = OmitTopLevelBracketCache<T>.CanOmit;
        }

        var writer = TinyhandRawWriter.CreateFromBytePool();
        try
        {
            TinyhandTreeConverter.FromBinaryToUtf8(rentMemory.Span, ref writer, options, omitTopLevelBracket);
            return writer.FlushAndGetRentMemory();
        }
        finally
        {
            rentMemory.Return();
            writer.Dispose();
        }
    }

    public static T? DeserializeObjectFromUtf8<T>(ReadOnlySpan<byte> utf8, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        var value = default(T);
        DeserializeObjectFromUtf8(utf8, ref value, options);
        return value;
    }

    public static void DeserializeObjectFromUtf8<T>(ReadOnlySpan<byte> utf8, scoped ref T? value, TinyhandSerializerOptions? options = null)
    where T : ITinyhandSerializable<T>
    {
        options = options ?? DefaultOptions;
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            bool omitTopLevelBracket; // = OmitTopLevelBracket<T>(options);
            if (options.Compose == TinyhandComposeOption.Strict)
            {
                omitTopLevelBracket = false;
            }
            else
            {
                omitTopLevelBracket = OmitTopLevelBracketCache<T>.CanOmit;
            }

            TinyhandTreeConverter.FromUtf8ToBinary(utf8, ref writer, omitTopLevelBracket);

            var reader = new TinyhandReader(writer);

            try
            {
                T.Deserialize(ref reader, ref value, options);
            }
            catch (TinyhandUnexpectedCodeException invalidCode)
            {// Invalid code
                var position = reader.Consumed;
                if (position > 0)
                {
                    position--;
                }

                // Get the Line/BytePosition from which the exception was thrown.
                var e = TinyhandTreeConverter.GetTextPositionFromBinaryPosition(utf8, position);
                TinyhandException? ex = invalidCode;

                if (e.LineNumber != 0)
                {
                    ex = new TinyhandException($"Unexpected element type, expected: {invalidCode.ExpectedType.ToString()} actual: {invalidCode.ActualType.ToString()} (Line:{e.LineNumber} BytePosition:{e.BytePositionInLine})");
                }

                throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value with the specified buffer writer.
    /// </summary>
    /// <param name="bufferWriter">The buffer writer to serialize with.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static void SerializeToUtf8<T>(IBufferWriter<byte> bufferWriter, T value, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        var binary = Serialize<T>(value, options);

        // Slow
        // TinyhandTreeConverter.FromBinaryToElement(binary, out var element, options);
        // TinyhandComposer.Compose(writer, element, options.Compose);

        var writer = new TinyhandRawWriter(bufferWriter);
        try
        {
            TinyhandTreeConverter.FromBinaryToUtf8(binary, ref writer, options);
            return;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value with the specified buffer writer.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] SerializeToUtf8<T>(T value, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        var binary = Serialize<T>(value, options);
        bool omitTopLevelBracket; // = OmitTopLevelBracket<T>(options);
        if (options.Compose == TinyhandComposeOption.Strict)
        {
            omitTopLevelBracket = false;
        }
        else
        {
            omitTopLevelBracket = OmitTopLevelBracketCache<T>.CanOmit;
        }

        // Slow
        // TinyhandTreeConverter.FromBinaryToElement(binary, out var element, options);
        // return TinyhandComposer.Compose(element, options.Compose);

        var writer = new TinyhandRawWriter(GetThreadStaticBuffer());
        try
        {
            TinyhandTreeConverter.FromBinaryToUtf8(binary, ref writer, options, omitTopLevelBracket);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value with the specified buffer writer.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static string SerializeToString<T>(T value, TinyhandSerializerOptions? options = null)
    {
        return TinyhandHelper.GetTextFromUtf8(SerializeToUtf8(value, options));
    }

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(ReadOnlySpan<byte> utf8, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;

        // Slow
        // var element = TinyhandParser.Parse(utf8, TinyhandParserOptions.TextSerialization);
        // return DeserializeFromElement<T>(element, options, cancellationToken);

        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            bool omitTopLevelBracket; // = OmitTopLevelBracket<T>(options);
            if (options.Compose == TinyhandComposeOption.Strict)
            {
                omitTopLevelBracket = false;
            }
            else
            {
                omitTopLevelBracket = OmitTopLevelBracketCache<T>.CanOmit;
            }

            TinyhandTreeConverter.FromUtf8ToBinary(utf8, ref writer, omitTopLevelBracket);

            var reader = new TinyhandReader(writer);

            try
            {
                return options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
            }
            catch (TinyhandUnexpectedCodeException invalidCode)
            {// Invalid code
                var position = reader.Consumed;
                if (position > 0)
                {
                    position--;
                }

                // Get the Line/BytePosition from which the exception was thrown.
                var e = TinyhandTreeConverter.GetTextPositionFromBinaryPosition(utf8, position);
                TinyhandException? ex = invalidCode;

                if (e.LineNumber != 0)
                {
                    ex = new TinyhandException($"Unexpected element type, expected: {invalidCode.ExpectedType.ToString()} actual: {invalidCode.ActualType.ToString()} (Line:{e.LineNumber} BytePosition:{e.BytePositionInLine})");
                }

                throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static T? DeserializeFromElement<T>(Element element, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        TinyhandTreeConverter.FromElementToBinary(element, out var binary, options);

        var reader = new TinyhandReader(binary);
        try
        {
            return options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
        }
        catch (TinyhandUnexpectedCodeException invalidCode)
        {// Invalid code
            var position = reader.Consumed;

            // Get the Element from which the exception was thrown.
            var e = TinyhandTreeConverter.GetElementFromPosition(element, position, options);
            TinyhandException? ex = invalidCode;

            if (e != null)
            {
                ex = new TinyhandException($"Unexpected element type, expected: {invalidCode.ExpectedType.ToString()} actual: {invalidCode.ActualType.ToString()} (Line:{e.LineNumber} BytePosition:{e.BytePositionInLine})");
            }

            throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
        }
    }

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(byte[] utf8, TinyhandSerializerOptions? options = null) => DeserializeFromUtf8<T>(utf8.AsSpan(), options);

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(ReadOnlyMemory<byte> utf8, TinyhandSerializerOptions? options = null) => DeserializeFromUtf8<T>(utf8.Span, options);

    /// <summary>
    /// Deserializes a value of a given type from a string (UTF-16).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf16">The string (UTF-16) to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromString<T>(string utf16, TinyhandSerializerOptions? options = null)
    {
        const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;
        byte[]? tempArray = null;

        Span<byte> utf8 = utf16.Length <= (ArrayPoolMaxSizeBeforeUsingNormalAlloc / TinyhandConstants.MaxExpansionFactorWhileTranscoding) ?
            tempArray = ArrayPool<byte>.Shared.Rent(utf16.Length * TinyhandConstants.MaxExpansionFactorWhileTranscoding) :
            new byte[TinyhandHelper.GetUtf8ByteCount(utf16.AsSpan())];

        try
        {
            int actualByteCount = TinyhandHelper.GetUtf8FromText(utf16.AsSpan(), utf8);
            utf8 = utf8.Slice(0, actualByteCount);
            return DeserializeFromUtf8<T>(utf8, options);
        }
        finally
        {
            if (tempArray != null)
            {
                utf8.Clear();
                ArrayPool<byte>.Shared.Return(tempArray);
            }
        }
    }

    private static class OmitTopLevelBracketCache<T>
    {
        public static readonly bool CanOmit = false;

        static OmitTopLevelBracketCache()
        {
            try
            {
                var value = TinyhandSerializer.Reconstruct<T>();
                var reader = new TinyhandReader(TinyhandSerializer.Serialize<T>(value));

                var code = reader.NextCode;
                if (code == MessagePackCode.Map16 || code == MessagePackCode.Map32 ||
                (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap))
                {// Map
                }
                else if (code == MessagePackCode.Array16 || code == MessagePackCode.Array32 ||
                (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray))
                {// Array
                }
                else
                {// Other
                    return;
                }

                if (reader.TrySkip() && reader.End)
                {// Single array or map.
                    CanOmit = true;
                }
            }
            catch
            {// Maybe TinyhandUnion
                CanOmit = true;
                return;
            }
        }
    }
}
