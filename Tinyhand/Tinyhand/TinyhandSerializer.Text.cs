// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arc.IO;
using MessagePack.LZ4;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Tinyhand
{
    public static partial class TinyhandSerializer
    {
        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
        public static void SerializeToUtf8<T>(IBufferWriter<byte> writer, T value, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? DefaultOptions;
            var binary = Serialize<T>(value, options, cancellationToken);
            TinyhandTreeConverter.FromBinary(binary, out var element, options);
            TinyhandComposer.Compose(writer, element, options.Compose);
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
            options = options ?? DefaultOptions;
            var binary = Serialize<T>(value, options, cancellationToken);
            TinyhandTreeConverter.FromBinary(binary, out var element, options);
            return TinyhandComposer.Compose(element, options.Compose);
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
            options = options ?? DefaultOptions;
            var element = TinyhandParser.Parse(utf8);
            return DeserializeFromElement<T>(element, options, cancellationToken);
        }

        public static T? DeserializeFromElement<T>(Element element, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? DefaultOptions;
            TinyhandTreeConverter.ToBinary(element, out var binary, out var debugInformation, options);

            var reader = new TinyhandReader(binary)
            {
                CancellationToken = cancellationToken,
            };

            try
            {
                return options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
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
            options = options ?? DefaultOptions;
            var element = TinyhandParser.Parse(utf16);
            return DeserializeFromElement<T>(element, options, cancellationToken);
        }
    }
}
