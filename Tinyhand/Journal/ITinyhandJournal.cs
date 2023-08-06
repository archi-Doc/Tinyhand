// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

public interface ITinyhandJournal
{// TinyhandGenerator, ValueLinkGenerator
    ITinyhandCrystal? Crystal { get; set; }

    uint CurrentPlane { get; set; }

    bool ReadRecord(ref TinyhandReader reader);
}
