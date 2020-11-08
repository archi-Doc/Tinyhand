// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand
{
    /// <summary>
    /// Interface for cumstom serialize/deserialize method.
    /// </summary>
    public interface ITinyhandSerialize
    {
        void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options);

        void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);
    }
}
