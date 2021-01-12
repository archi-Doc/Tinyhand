// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Tinyhand.Generator
{
    public class TinyhandTextSerialization
    {
        public TinyhandTextSerialization(TinyhandObject obj)
        {
            this.Body = obj.Body;
            this.Object = obj;
        }

        public TinyhandBody Body { get; }

        public TinyhandObject Object { get; }

        public void CheckKey()
        {
            var keyNumber = 0;

            foreach (var x in this.Object.MembersWithFlag(TinyhandObjectFlag.SerializeTarget))
            {
                if (x.KeyAttribute?.StringKey is string s)
                {// String key
                    s = s.Trim();
                    if (!this.IsValidIdentifier(s))
                    {// Not a valid identifier
                        var s2 = "key" + keyNumber++;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_InvalidIdentifier, x.KeyVisceralAttribute?.Location, s, s2);
                        s = s2;
                    }

                    x.KeyAttribute.SetKey(s);
                }
            }
        }

        public bool IsValidIdentifier(string identifier)
        {
            var s = identifier.AsSpan();
            if (s.Length == 0)
            {
                return false;
            }

            for (var n = 0; n < s.Length; n++)
            {
                if (s[n] == '{' || s[n] == '}' || s[n] == '"' || s[n] == '=' ||
                    s[n] == '/' || s[n] == ' ' || s[n] == '\r' || s[n] == '\n' ||
                    s[n] == '\\' || s[n] == '+' || s[n] == '-' || s[n] == '*')
                {// Invalid character
                    return false;
                }
            }

            if (s[0] >= '0' && s[0] <= '9')
            {// Number
                return false;
            }

            if (this.reservedString.Contains(identifier))
            {// Reserved
                return false;
            }

            return true;
        }

#pragma warning disable SA1500
        private HashSet<string> reservedString = new() { "null", "true", "false", "Bool", "I32", "I64", "U32", "U64",
          "Single", "Double", "String", "Key", "Array", "Map", "Required", "Optional", };
#pragma warning restore SA1500
    }
}
