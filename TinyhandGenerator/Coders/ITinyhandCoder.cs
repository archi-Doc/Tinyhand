// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders;

public interface ITinyhandCoder
{
    /// <summary>
    /// Outputs the code to serialize an object.
    /// </summary>
    /// <param name="ssb">The scoping string builder to output code.</param>
    /// <param name="info">The generator information.</param>
    void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info);

    /// <summary>
    /// Outputs the code to deserialize an object.
    /// </summary>
    /// <param name="ssb">The scoping string builder to output code.</param>
    /// <param name="info">The generator information.</param>
    /// <param name="nilChecked">True if the next code is non-Nil.</param>
    void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked = false);

    /// <summary>
    /// Outputs the code to reconstruct an object (create a new instance).
    /// </summary>
    /// <param name="ssb">The scoping string builder to output code.</param>
    /// <param name="info">The generator information.</param>
    void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info);

    /// <summary>
    /// Outputs the code to clone an object (create a new instance).
    /// </summary>
    /// <param name="ssb">The scoping string builder to output code.</param>
    /// <param name="info">The generator information.</param>
    /// <param name="sourceObject">The name of the source object.</param>
    void CodeClone(ScopingStringBuilder ssb, GeneratorInformation info, string sourceObject);
}
