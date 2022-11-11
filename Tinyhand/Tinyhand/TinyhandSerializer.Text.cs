// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arc.IO;
using MessagePack.LZ4;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Tinyhand;

public static partial class TinyhandSerializer
{
    /// <summary>
    /// Serializes a given value with the specified buffer writer.
    /// </summary>
    /// <param name="bufferWriter">The buffer writer to serialize with.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static void SerializeToUtf8<T>(IBufferWriter<byte> bufferWriter, T value, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        options = options ?? DefaultOptions;
        var binary = Serialize<T>(value, options, cancellationToken);

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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] SerializeToUtf8<T>(T value, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        options = options ?? DefaultOptions;
        var binary = Serialize<T>(value, options, cancellationToken);
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

        var writer = new TinyhandRawWriter(initialBuffer);
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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A byte array with the serialized value (UTF-8).</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static string SerializeToString<T>(T value, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        return TinyhandHelper.GetTextFromUtf8(SerializeToUtf8(value, options, cancellationToken));
    }

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(ReadOnlySpan<byte> utf8, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        options = options ?? DefaultOptions;

        // Slow
        // var element = TinyhandParser.Parse(utf8, TinyhandParserOptions.TextSerialization);
        // return DeserializeFromElement<T>(element, options, cancellationToken);

        var writer = new TinyhandWriter(initialBuffer) { CancellationToken = cancellationToken };
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

            var reader = new TinyhandReader(writer) { CancellationToken = cancellationToken };

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

    /*/// <summary>
    /// Deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="reuse">The existing instance (TinyhandObject attribute required) to reuse.</param>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeWithFromUtf8<T>(T reuse, ReadOnlySpan<byte> utf8, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        options = options ?? DefaultOptions;

        var writer = new TinyhandWriter(initialBuffer) { CancellationToken = cancellationToken };
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

            var reader = new TinyhandReader(writer) { CancellationToken = cancellationToken };

            try
            {
                return options.Resolver.GetFormatterExtra<T>().Deserialize(reuse, ref reader, options);
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
    }*/

    public static T? DeserializeFromElement<T>(Element element, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        options = options ?? DefaultOptions;
        TinyhandTreeConverter.FromElementToBinary(element, out var binary, options);

        var reader = new TinyhandReader(binary)
        {
            CancellationToken = cancellationToken,
        };

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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(byte[] utf8, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default) => DeserializeFromUtf8<T>(utf8.AsSpan(), options, cancellationToken);

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromUtf8<T>(ReadOnlyMemory<byte> utf8, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default) => DeserializeFromUtf8<T>(utf8.Span, options, cancellationToken);

    /*/// <summary>
    /// Reuse an existing instance and deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="reuse">The existing instance (TinyhandObject attribute required) to reuse.</param>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeWithFromUtf8<T>(T reuse, byte[] utf8, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default) => DeserializeWithFromUtf8<T>(reuse, utf8.AsSpan(), options, cancellationToken);

    /// <summary>
    /// Reuse an existing instance and deserializes a value of a given type from a sequence of bytes (UTF-8).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="reuse">The existing instance (TinyhandObject attribute required) to reuse.</param>
    /// <param name="utf8">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeWithFromUtf8<T>(T reuse, ReadOnlyMemory<byte> utf8, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default) => DeserializeWithFromUtf8<T>(reuse, utf8.Span, options, cancellationToken);*/

    /// <summary>
    /// Deserializes a value of a given type from a string (UTF-16).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="utf16">The string (UTF-16) to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeFromString<T>(string utf16, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
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
            return DeserializeFromUtf8<T>(utf8, options, cancellationToken);
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

    /*/// <summary>
    /// Reuse an existing instance and deserializes a value of a given type from a string (UTF-16).
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="reuse">The existing instance (TinyhandObject attribute required) to reuse.</param>
    /// <param name="utf16">The string (UTF-16) to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? DeserializeWithFromString<T>(T reuse, string utf16, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
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
            return DeserializeWithFromUtf8<T>(reuse, utf8, options, cancellationToken);
        }
        finally
        {
            if (tempArray != null)
            {
                utf8.Clear();
                ArrayPool<byte>.Shared.Return(tempArray);
            }
        }
    }*/

    /*private static bool OmitTopLevelBracket<T>(TinyhandSerializerOptions options)
    {
        if (options.Compose == TinyhandComposeOption.Strict)
        {
            return false;
        }

        return typeToOmitTopLevelBracket.GetOrAdd(typeof(T), static x =>
        {// Determines if the object is a single array or map, and the top level bracket can be omitted.
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
                    return false;
                }

                if (reader.TrySkip() && reader.End)
                {// Single array or map.
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        });
    }

    private static ConcurrentDictionary<Type, bool> typeToOmitTopLevelBracket = new();*/

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
            {
            }
        }
    }
}
