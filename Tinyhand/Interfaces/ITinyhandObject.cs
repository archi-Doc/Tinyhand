// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

public interface ITinyhandObject<T>
{
    static abstract void Serialize(ref TinyhandWriter writer, scoped ref T? value, TinyhandSerializerOptions options);

    static abstract void Deserialize(ref TinyhandReader reader, scoped ref T? value, TinyhandSerializerOptions options);
}
