// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Tinyhand.IO;
using Tinyhand.Resolvers;

namespace Tinyhand;

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
        /// Serialize all members.
        /// </summary>
        All,

        /// <summary>
        /// Serialize members with the selection property set to true.
        /// </summary>
        Selection,

        /// <summary>
        /// Serialize an object for signature generation. Level is enabled and WriteArrayHeader() is skipped.
        /// </summary>
        Signature,
    }

    [Flags]
    public enum Flags
    {
        /// <summary>
        /// Compress the data using the Lz4 algorithm.
        /// </summary>
        Lz4Compress = 1 << 0,

        /// <summary>
        /// Unload data (the actual unloading process will be implemented by the application side).
        /// </summary>
        Unload = 1 << 1,
    }

    public static TinyhandSerializerOptions Standard { get; } = new TinyhandSerializerOptions(StandardResolver.Instance);

    public static TinyhandSerializerOptions Compatible { get; } = new TinyhandSerializerOptions(CompatibleResolver.Instance);

    public static TinyhandSerializerOptions Lz4 { get; } = Standard with { SerializationFlags = Flags.Lz4Compress, };

    public static TinyhandSerializerOptions Unload { get; } = Standard with { SerializationFlags = Flags.Unload, };

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
    /// Gets the serialization flags.
    /// </summary>
    public Flags SerializationFlags { get; init; }

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
    public Mode SerializationMode { get; init; } = Mode.All;

    public bool IsAllMode => this.SerializationMode == Mode.All;

    public bool IsSelectionMode => this.SerializationMode == Mode.Selection;

    public bool IsSignatureMode => this.SerializationMode == Mode.Signature;

    /// <summary>
    /// Gets a value indicating whether the option uses Standard resolver or not.
    /// </summary>
    public bool IsStandardResolver => this.Resolver == StandardResolver.Instance;

    public bool HasLz4CompressFlag => this.SerializationFlags.HasFlag(Flags.Lz4Compress);

    public bool HasUnloadFlag => this.SerializationFlags.HasFlag(Flags.Unload);
}
