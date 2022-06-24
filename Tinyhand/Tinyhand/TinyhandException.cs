// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Tinyhand;

public class TinyhandException : Exception
{
    public TinyhandException(string message)
        : base(message)
    {
    }

    public TinyhandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class TinyhandUnexpectedCodeException : TinyhandException
{
    public TinyhandUnexpectedCodeException(string message, MessagePackType actual, MessagePackType expected)
        : base(message)
    {
        this.ActualType = actual;
        this.ExpectedType = expected;
    }

    public TinyhandUnexpectedCodeException(string message, MessagePackType actual, MessagePackType expected, Exception innerException)
        : base(message, innerException)
    {
        this.ActualType = actual;
        this.ExpectedType = expected;
    }

    public MessagePackType ActualType { get; }

    public MessagePackType ExpectedType { get; }
}
