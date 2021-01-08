// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arc.IO;
using MessagePack.LZ4;
using Tinyhand.Tree;

#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Tinyhand
{
    public static partial class TinyhandSerializer
    {
        /// <summary>
        /// Gets or sets the default set of options to use when not explicitly specified for a method call.
        /// </summary>
        /// <value>The default value is <see cref="TinyhandTextSerializerOptions.Standard"/>.</value>
        /// <remarks>
        /// This is an AppDomain or process-wide setting.
        /// If you're writing a library, you should NOT set or rely on this property but should instead pass
        /// in <see cref="TinyhandTextSerializerOptions.Standard"/> (or the required options) explicitly to every method call
        /// to guarantee appropriate behavior in any application.
        /// If you are an app author, realize that setting this property impacts the entire application so it should only be
        /// set once, and before any use of <see cref="TinyhandSerializer"/> occurs.
        /// </remarks>
        public static TinyhandTextSerializerOptions TextDefaultOptions { get; set; } = TinyhandTextSerializerOptions.Standard;

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
        public static void TextSerialize<T>(IBufferWriter<byte> writer, T value, TinyhandTextSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            Element element;

            options = options ?? TextDefaultOptions;
            try
            {
                options.Resolver.GetFormatter<T>().Serialize(out element, value, options);
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
            }

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
        public static byte[] TextSerialize<T>(T value, TinyhandTextSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            Element element;

            options = options ?? TextDefaultOptions;
            try
            {
                options.Resolver.GetFormatter<T>().Serialize(out element, value, options);
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
            }

            return TinyhandComposer.Compose(element, options.Compose);
        }
    }
}
