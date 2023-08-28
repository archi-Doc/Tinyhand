// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Tinyhand.IO;
using Tinyhand.Resolvers;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

public enum TinyhandCompression
{
    None,
    Lz4,
}

public static class TinyhandSerializerOptionsExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DeserializeAndReconstruct<T>(this TinyhandSerializerOptions options, ref TinyhandReader reader)
    {
        ITinyhandFormatter<T>? formatter;

        formatter = options.Resolver.TryGetFormatter<T>();
        if (formatter == null)
        {
            Throw(typeof(T), options.Resolver);
        }

        return formatter!.Deserialize(ref reader, options) ?? formatter!.Reconstruct(options);
    }

    private static void Throw(Type t, IFormatterResolver resolver)
    {
        throw new FormatterNotRegisteredException(t.FullName + " is not registered in resolver: " + resolver.GetType());
    }
}

public record TinyhandSerializerOptions
{
    public enum Mode
    {
        /// <summary>
        /// Standard mode.
        /// </summary>
        Standard,

        /// <summary>
        /// Serialize members with the selection property set to true.
        /// </summary>
        Selection,

        /// <summary>
        /// Serialize an object for signature generation. Level is enabled and WriteArrayHeader() is skipped.
        /// </summary>
        Signature,
    }

    public static TinyhandSerializerOptions Standard { get; } = new TinyhandSerializerOptions(StandardResolver.Instance);

    public static TinyhandSerializerOptions Compatible { get; } = new TinyhandSerializerOptions(CompatibleResolver.Instance);

    public static TinyhandSerializerOptions Lz4 { get; } = Standard with { Compression = TinyhandCompression.Lz4, };

    public static TinyhandSerializerOptions Selection { get; } = Standard with { SerializationMode = Mode.Selection, };

    public static TinyhandSerializerOptions Signature { get; } = Standard with { SerializationMode = Mode.Signature, };

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandSerializerOptions"/> class.
    /// </summary>
    /// <param name="resolver">The new value for the <see cref="Resolver"/>.</param>
    protected internal TinyhandSerializerOptions(IFormatterResolver resolver)
    {
        this.Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// Gets the resolver to use for complex types.
    /// </summary>
    /// <value>An instance of <see cref="IFormatterResolver"/>. Never <c>null</c>.</value>
    /// <exception cref="ArgumentNullException">Thrown if an attempt is made to set this property to <c>null</c>.</exception>
    public IFormatterResolver Resolver { get; init; }

    /// <summary>
    /// Gets the compression scheme to apply to serialized sequences.
    /// </summary>
    /// <remarks>
    /// When set to something other than <see cref="TinyhandCompression.None"/>,
    /// deserialization can still work on uncompressed sequences,
    /// and serialization may not compress if msgpack sequences are short enough that compression would not likely be advantageous.
    /// </remarks>
    public TinyhandCompression Compression { get; init; } = TinyhandCompression.None;

    /// <summary>
    /// Gets the security-related options for deserializing messagepack sequences.
    /// </summary>
    /// <value>
    /// The default value is to use <see cref="TinyhandSecurity.TrustedData"/>.
    /// </value>
    public TinyhandSecurity Security { get; init; } = TinyhandSecurity.TrustedData;

    /// <summary>
    /// Gets the compose option.
    /// </summary>
    public TinyhandComposeOption Compose { get; init; } = TinyhandComposeOption.Standard;

    /*/// <summary>
    /// Gets the serialization plane.
    /// </summary>
    public uint Plane { get; init; }*/

    /// <summary>
    /// Gets the serialization mode.
    /// </summary>
    public Mode SerializationMode { get; init; } = Mode.Standard;

    public bool IsStandardMode => this.SerializationMode == Mode.Standard;

    public bool IsSelectionMode => this.SerializationMode == Mode.Selection;

    public bool IsSignatureMode => this.SerializationMode == Mode.Signature;

    /// <summary>
    /// Gets a value indicating whether the option uses Standard resolver or not.
    /// </summary>
    public bool IsStandardResolver => this.Resolver == StandardResolver.Instance;
}
