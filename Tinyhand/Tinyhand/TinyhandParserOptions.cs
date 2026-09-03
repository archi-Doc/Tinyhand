// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

/// <summary>
/// Configures contextual information and assignment handling when parsing Tinyhand text.
/// </summary>
public record TinyhandParserOptions
{
    public static TinyhandParserOptions Standard { get; } = new TinyhandParserOptions();

    public static TinyhandParserOptions ContextualInformation { get; } = Standard with { ParseContextualInformation = true, };

    public static TinyhandParserOptions TextSerialization { get; } = Standard with { TextSerializationMode = true, };

    /// <summary>
    /// Gets a value indicating whether comments and line breaks are retained in the syntax tree.
    /// </summary>
    public bool ParseContextualInformation { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether string assignment keys are converted to identifiers for text serialization.
    /// </summary>
    public bool TextSerializationMode { get; init; } = false;
}
