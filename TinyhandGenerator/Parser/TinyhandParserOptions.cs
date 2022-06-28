// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

internal class TinyhandParserOptions
{
    public static TinyhandParserOptions Standard => new TinyhandParserOptions();

    public static TinyhandParserOptions ContextualInformation => Standard.WithContextualInformation(true);

    public static TinyhandParserOptions TextSerialization => Standard.WithTextSerialization(true);

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandParserOptions"/> class.
    /// </summary>
    protected internal TinyhandParserOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandParserOptions"/> class
    /// with members initialized from an existing instance.
    /// </summary>
    /// <param name="copyFrom">The options to copy from.</param>
    protected TinyhandParserOptions(TinyhandParserOptions copyFrom)
    {
        this.ParseContextualInformation = copyFrom.ParseContextualInformation;
        this.TextSerializationMode = copyFrom.TextSerializationMode;
    }

    /// <summary>
    /// Gets a value indicating whether or not to parse contextual information (comment, line feed).
    /// </summary>
    public bool ParseContextualInformation { get; private set; } = false;

    /// <summary>
    /// Gets a value indicating whether text serialization mode is active (the left element of the assigment is converted to an identifier).
    /// </summary>
    public bool TextSerializationMode { get; private set; } = false;

    /// <summary>
    /// Gets a copy of these options with the <see cref="ParseContextualInformation"/> property set to a new value.
    /// </summary>
    /// <param name="parseContextualInformation">The new value for the <see cref="ParseContextualInformation"/> property.</param>
    /// <returns>The new instance; or the original if the value is unchanged.</returns>
    public TinyhandParserOptions WithContextualInformation(bool parseContextualInformation)
    {
        if (this.ParseContextualInformation == parseContextualInformation)
        {
            return this;
        }

        var result = this.Clone();
        result.ParseContextualInformation = parseContextualInformation;
        return result;
    }

    /// <summary>
    /// Gets a copy of these options with the <see cref="TextSerializationMode"/> property set to a new value.
    /// </summary>
    /// <param name="textSerializationMode">The new value for the <see cref="TextSerializationMode"/> property.</param>
    /// <returns>The new instance; or the original if the value is unchanged.</returns>
    public TinyhandParserOptions WithTextSerialization(bool textSerializationMode)
    {
        if (this.TextSerializationMode == textSerializationMode)
        {
            return this;
        }

        var result = this.Clone();
        result.TextSerializationMode = textSerializationMode;
        return result;
    }

    /// <summary>
    /// Creates a clone of this instance with the same properties set.
    /// </summary>
    /// <returns>The cloned instance. Guaranteed to be a new instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if this instance is a derived type that doesn't override this method.</exception>
    protected virtual TinyhandParserOptions Clone()
    {
        if (this.GetType() != typeof(TinyhandParserOptions))
        {
            throw new NotSupportedException($"The derived type {this.GetType().FullName} did not override the {nameof(this.Clone)} method as required.");
        }

        return new TinyhandParserOptions(this);
    }
}
