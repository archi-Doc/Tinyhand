// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

public record TinyhandParserOptions
{
    public static TinyhandParserOptions Standard { get; } = new TinyhandParserOptions();

    public static TinyhandParserOptions ContextualInformation { get; } = Standard with { ParseContextualInformation = true, };

    public static TinyhandParserOptions TextSerialization { get; } = Standard with { TextSerializationMode = true, };

    /// <summary>
    /// Gets a value indicating whether or not to parse contextual information (comment, line feed).
    /// </summary>
    public bool ParseContextualInformation { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether text serialization mode is active (the left element of the assigment is converted to an identifier).
    /// </summary>
    public bool TextSerializationMode { get; init; } = false;
}
