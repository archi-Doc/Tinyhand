// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Threading;
using Arc;
using Arc.Collections;
using Arc.IO;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Tinyhand;

/// <summary>
/// Provides UTF-8 and UTF-16 text serialization and syntax-tree deserialization.
/// </summary>
public static partial class TinyhandSerializer
{
    /// <summary>
    /// Serializes a value as UTF-8 Tinyhand text.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] SerializeObjectToUtf8<T>(T value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? TinyhandSerializerOptions.ConvertToString;
        var omitTopLevelBracket = OmitTopLevelBracket<T>(options);

        if (options.HasLz4CompressFlag)
        {// The compression uses the second thread-static buffer, so the binary goes through an array.
            var binary = SerializeObject(value, options);
            return BinaryToUtf8Array(binary, options, omitTopLevelBracket);
        }

        // The binary is written to the second thread-static buffer and the text to the first one: no intermediate array.
        var binaryWriter = new TinyhandWriter(GetThreadStaticBuffer2());
        try
        {
            SerializeObject(ref binaryWriter, value, options);
            binaryWriter.FlushAndGetReadOnlySpan(out var binary, out _);
            return BinaryToUtf8Array(binary, options, omitTopLevelBracket);
        }
        finally
        {
            binaryWriter.Dispose();
        }
    }

    /// <summary>
    /// Serializes a Tinyhand object as UTF-8 text in pooled memory.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>The UTF-8 text. Return the memory to its pool after use.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static BytePool.RentMemory SerializeObjectToUtf8RentMemory<T>(T value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? TinyhandSerializerOptions.ConvertToString;
        var rentMemory = SerializeObjectToRentMemory(value, options);
        var omitTopLevelBracket = OmitTopLevelBracket<T>(options);

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
        options = options ?? TinyhandSerializerOptions.ConvertToString;
        var omitTopLevelBracket = OmitTopLevelBracket<T>(options);

        var buffer = TinyhandTreeConverter.BinaryBuffer.Acquire();
        try
        {
            TinyhandTreeConverter.FromUtf8ToBinary(utf8, omitTopLevelBracket, ref buffer);
            var reader = new TinyhandReader(buffer.Span);

            try
            {
                T.Deserialize(ref reader, ref value, options);
            }
            catch (TinyhandUnexpectedCodeException invalidCode)
            {// Invalid code
                throw CreateUnexpectedCodeException<T>(utf8, reader.Consumed, omitTopLevelBracket, invalidCode);
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
        }
        finally
        {
            buffer.Release();
        }
    }

    /// <summary>
    /// Serializes a value as UTF-8 Tinyhand text.
    /// </summary>
    /// <param name="bufferWriter">The buffer writer to serialize with.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static void SerializeToUtf8<T>(IBufferWriter<byte> bufferWriter, T value, TinyhandSerializerOptions? options = null)
    {
        options = options ?? TinyhandSerializerOptions.ConvertToString;

        // The same layout as SerializeToUtf8<T>(T), so the text can be read back by DeserializeFromUtf8<T>.
        var omitTopLevelBracket = OmitTopLevelBracket<T>(options);

        // Slow
        // TinyhandTreeConverter.FromBinaryToElement(binary, out var element, options);
        // TinyhandComposer.Compose(writer, element, options.Compose);

        if (options.HasLz4CompressFlag)
        {
            var binary = Serialize<T>(value, options);
            var writer = new TinyhandRawWriter(bufferWriter);
            try
            {
                TinyhandTreeConverter.FromBinaryToUtf8(binary, ref writer, options, omitTopLevelBracket);
                writer.Flush(); // Commit the last segment to the buffer writer.
            }
            finally
            {
                writer.Dispose();
            }

            return;
        }

        var binaryWriter = new TinyhandWriter(GetThreadStaticBuffer2());
        try
        {
            Serialize(ref binaryWriter, value, options);
            binaryWriter.FlushAndGetReadOnlySpan(out var binary, out _);
            var writer = new TinyhandRawWriter(bufferWriter);
            try
            {
                TinyhandTreeConverter.FromBinaryToUtf8(binary, ref writer, options, omitTopLevelBracket);
                writer.Flush(); // Commit the last segment to the buffer writer.
            }
            finally
            {
                writer.Dispose();
            }
        }
        finally
        {
            binaryWriter.Dispose();
        }
    }

    /// <summary>
    /// Serializes a value as UTF-8 Tinyhand text.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] SerializeToUtf8<T>(T value, TinyhandSerializerOptions? options = null)
    {
        options = options ?? TinyhandSerializerOptions.ConvertToString;
        var omitTopLevelBracket = OmitTopLevelBracket<T>(options);

        // Slow
        // TinyhandTreeConverter.FromBinaryToElement(binary, out var element, options);
        // return TinyhandComposer.Compose(element, options.Compose);

        if (options.HasLz4CompressFlag)
        {// The compression uses the second thread-static buffer, so the binary goes through an array.
            var binary = Serialize<T>(value, options);
            return BinaryToUtf8Array(binary, options, omitTopLevelBracket);
        }

        // The binary is written to the second thread-static buffer and the text to the first one: no intermediate array.
        var binaryWriter = new TinyhandWriter(GetThreadStaticBuffer2());
        try
        {
            Serialize(ref binaryWriter, value, options);
            binaryWriter.FlushAndGetReadOnlySpan(out var binary, out _);
            return BinaryToUtf8Array(binary, options, omitTopLevelBracket);
        }
        finally
        {
            binaryWriter.Dispose();
        }
    }

    /// <summary>
    /// Serializes a value as a UTF-16 Tinyhand string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>The serialized UTF-16 string.</returns>
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
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(ReadOnlySpan<byte> utf8, TinyhandSerializerOptions? options = null)
    {
        options = options ?? TinyhandSerializerOptions.ConvertToString;
        var omitTopLevelBracket = OmitTopLevelBracket<T>(options);

        // Slow
        // var element = TinyhandParser.Parse(utf8, TinyhandParserOptions.TextSerialization);
        // return DeserializeFromElement<T>(element, options, cancellationToken);

        var buffer = TinyhandTreeConverter.BinaryBuffer.Acquire();
        try
        {
            TinyhandTreeConverter.FromUtf8ToBinary(utf8, omitTopLevelBracket, ref buffer);
            var reader = new TinyhandReader(buffer.Span);

            try
            {
                return options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
            }
            catch (TinyhandUnexpectedCodeException invalidCode)
            {// Invalid code
                throw CreateUnexpectedCodeException<T>(utf8, reader.Consumed, omitTopLevelBracket, invalidCode);
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
        }
        finally
        {
            buffer.Release();
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
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(byte[] utf8, TinyhandSerializerOptions? options = null) => DeserializeFromUtf8<T>(utf8.AsSpan(), options);

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(ReadOnlyMemory<byte> utf8, TinyhandSerializerOptions? options = null) => DeserializeFromUtf8<T>(utf8.Span, options);

    /// <summary>
    /// Attempts to deserialize a value of a given type from a string (UTF-16). Returns the default value if deserialization fails.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf16">The string (UTF-16) to deserialize from.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>The deserialized value, or the default value of <typeparamref name="T"/> if deserialization fails.</returns>
    public static T? TryDeserializeFromString<T>(ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options = null)
    {
        try
        {
            return DeserializeFromString<T>(utf16, options);
        }
        catch
        {
            return default;
        }
    }

    public static T? TryParseOrDeserializeFromString<T>(ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>, IStringConvertible<T>
    {
        if (utf16.Length >= 2 &&
            utf16[0] == TinyhandConstants.OpenBraceChar &&
            utf16[^1] == TinyhandConstants.CloseBraceChar)
        {// {Text}
            try
            {
                options ??= TinyhandSerializerOptions.ConvertToString;
                if (OmitTopLevelBracket<T>(options))
                {
                    // The ordinary reader adds the omitted outer array/map itself.
                    utf16 = utf16.Slice(1, utf16.Length - 2);
                }

                return DeserializeFromString<T>(utf16, options);
            }
            catch
            {
                return default;
            }
        }
        else
        {// text
            T.TryParse(utf16, out var obj, out _);
            return obj;
        }
    }

    /// <summary>
    /// Deserializes a value of a given type from a string (UTF-16).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf16">The string (UTF-16) to deserialize from.</param>
    /// <param name="options">The options, or <see langword="null"/> to use <see cref="TinyhandSerializerOptions.ConvertToString"/>.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromString<T>(ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options = null)
    {
        const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;
        byte[]? tempArray = null;

        Span<byte> utf8 = utf16.Length <= (ArrayPoolMaxSizeBeforeUsingNormalAlloc / TinyhandConstants.MaxExpansionFactorWhileTranscoding) ?
            tempArray = ArrayPool<byte>.Shared.Rent(utf16.Length * TinyhandConstants.MaxExpansionFactorWhileTranscoding) :
            new byte[TinyhandHelper.GetUtf8ByteCount(utf16)];

        try
        {
            int actualByteCount = TinyhandHelper.GetUtf8FromText(utf16, utf8);
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

    /// <summary>
    /// Converts a binary to UTF-8 text using the first thread-static buffer and returns the text as a new array.
    /// </summary>
    private static byte[] BinaryToUtf8Array(ReadOnlySpan<byte> binary, TinyhandSerializerOptions options, bool omitTopLevelBracket)
    {
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
    /// Creates the exception for a <see cref="TinyhandUnexpectedCodeException"/>, with the line and byte position of the offending text when available.
    /// </summary>
    private static TinyhandException CreateUnexpectedCodeException<T>(ReadOnlySpan<byte> utf8, int consumed, bool omitTopLevelBracket, TinyhandUnexpectedCodeException invalidCode)
    {
        // The reader has consumed the unexpected code, so the code starts one byte before.
        var position = consumed;
        if (position > 0)
        {
            position--;
        }

        // Get the Line/BytePosition from which the exception was thrown.
        var e = TinyhandTreeConverter.GetTextPositionFromBinaryPosition(utf8, position, omitTopLevelBracket);
        TinyhandException? ex = invalidCode;

        if (e.LineNumber != 0)
        {
            ex = new TinyhandException($"Unexpected element type, expected: {invalidCode.ExpectedType.ToString()} actual: {invalidCode.ActualType.ToString()} (Line:{e.LineNumber} BytePosition:{e.BytePositionInLine})");
        }

        return new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
    }

    private static bool OmitTopLevelBracket<T>(TinyhandSerializerOptions options)
        => options.Compose != TinyhandComposeOption.Strict && OmitTopLevelBracketCache<T>.CanOmit;

    private static class OmitTopLevelBracketCache<T>
    {
        public static readonly bool CanOmit = false;

        static OmitTopLevelBracketCache()
        {
            CanOmit = typeof(ITinyhandSingleLayoutSerializable).IsAssignableFrom(typeof(T));

            // The following code was removed because creating an object for the check has side effects.
            /*try
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
            }*/
        }
    }
}
