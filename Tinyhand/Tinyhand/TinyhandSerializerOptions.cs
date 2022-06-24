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

public class TinyhandSerializerOptions
{
    public static TinyhandSerializerOptions Standard => new TinyhandSerializerOptions(StandardResolver.Instance);

    public static TinyhandSerializerOptions Compatible => new TinyhandSerializerOptions(CompatibleResolver.Instance);

    public static TinyhandSerializerOptions Lz4 => Standard.WithCompression(TinyhandCompression.Lz4);

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandSerializerOptions"/> class.
    /// </summary>
    /// <param name="resolver">The new value for the <see cref="Resolver"/>.</param>
    protected internal TinyhandSerializerOptions(IFormatterResolver resolver)
    {
        this.Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandSerializerOptions"/> class
    /// with members initialized from an existing instance.
    /// </summary>
    /// <param name="copyFrom">The options to copy from.</param>
    protected TinyhandSerializerOptions(TinyhandSerializerOptions copyFrom)
    {
        this.Resolver = copyFrom.Resolver;
        this.Compression = copyFrom.Compression;
        this.Security = copyFrom.Security;
        this.Compose = copyFrom.Compose;
    }

    /// <summary>
    /// Gets the resolver to use for complex types.
    /// </summary>
    /// <value>An instance of <see cref="IFormatterResolver"/>. Never <c>null</c>.</value>
    /// <exception cref="ArgumentNullException">Thrown if an attempt is made to set this property to <c>null</c>.</exception>
    public IFormatterResolver Resolver { get; private set; }

    /// <summary>
    /// Gets the compression scheme to apply to serialized sequences.
    /// </summary>
    /// <remarks>
    /// When set to something other than <see cref="TinyhandCompression.None"/>,
    /// deserialization can still work on uncompressed sequences,
    /// and serialization may not compress if msgpack sequences are short enough that compression would not likely be advantageous.
    /// </remarks>
    public TinyhandCompression Compression { get; private set; }

    /// <summary>
    /// Gets the security-related options for deserializing messagepack sequences.
    /// </summary>
    /// <value>
    /// The default value is to use <see cref="TinyhandSecurity.TrustedData"/>.
    /// </value>
    public TinyhandSecurity Security { get; private set; } = TinyhandSecurity.TrustedData;

    /// <summary>
    /// Gets the compose option.
    /// </summary>
    public TinyhandComposeOption Compose { get; private set; }

    /// <summary>
    /// Gets a copy of these options with the <see cref="Resolver"/> property set to a new value.
    /// </summary>
    /// <param name="resolver">The new value for the <see cref="Resolver"/>.</param>
    /// <returns>The new instance; or the original if the value is unchanged.</returns>
    public TinyhandSerializerOptions WithResolver(IFormatterResolver resolver)
    {
        if (this.Resolver == resolver)
        {
            return this;
        }

        var result = this.Clone();
        result.Resolver = resolver;
        return result;
    }

    /// <summary>
    /// Gets a copy of these options with the <see cref="Compression"/> property set to a new value.
    /// </summary>
    /// <param name="compression">The new value for the <see cref="Compression"/> property.</param>
    /// <returns>The new instance; or the original if the value is unchanged.</returns>
    public TinyhandSerializerOptions WithCompression(TinyhandCompression compression)
    {
        if (this.Compression == compression)
        {
            return this;
        }

        var result = this.Clone();
        result.Compression = compression;
        return result;
    }

    /// <summary>
    /// Gets a copy of these options with the <see cref="Security"/> property set to a new value.
    /// </summary>
    /// <param name="security">The new value for the <see cref="Security"/> property.</param>
    /// <returns>The new instance; or the original if the value is unchanged.</returns>
    public TinyhandSerializerOptions WithSecurity(TinyhandSecurity security)
    {
        if (security is null)
        {
            throw new ArgumentNullException(nameof(security));
        }

        if (this.Security == security)
        {
            return this;
        }

        var result = this.Clone();
        result.Security = security;
        return result;
    }

    /// <summary>
    /// Gets a copy of these options with the <see cref="Compose"/> property set to a new value.
    /// </summary>
    /// <param name="compose">The new value for the <see cref="Compose"/> property.</param>
    /// <returns>The new instance; or the original if the value is unchanged.</returns>
    public TinyhandSerializerOptions WithCompose(TinyhandComposeOption compose)
    {
        if (this.Compose == compose)
        {
            return this;
        }

        var result = this.Clone();
        result.Compose = compose;
        return result;
    }

    /// <summary>
    /// Creates a clone of this instance with the same properties set.
    /// </summary>
    /// <returns>The cloned instance. Guaranteed to be a new instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if this instance is a derived type that doesn't override this method.</exception>
    protected virtual TinyhandSerializerOptions Clone()
    {
        if (this.GetType() != typeof(TinyhandSerializerOptions))
        {
            throw new NotSupportedException($"The derived type {this.GetType().FullName} did not override the {nameof(this.Clone)} method as required.");
        }

        return new TinyhandSerializerOptions(this);
    }
}
