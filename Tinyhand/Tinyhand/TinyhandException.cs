// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Tinyhand
{
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
}
