﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Tinyhand.Generator
{
    public static class AutomataKeyFake
    {
        public static ulong GetKey(ref ReadOnlySpan<byte> span)
        {
            ulong key;

            unchecked
            {
                if (span.Length >= 8)
                {
                    key = SafeBitConverterFake.ToUInt64(span);
                    span = span.Slice(8);
                }
                else
                {
                    switch (span.Length)
                    {
                        case 1:
                            {
                                key = span[0];
                                span = span.Slice(1);
                                break;
                            }

                        case 2:
                            {
                                key = SafeBitConverterFake.ToUInt16(span);
                                span = span.Slice(2);
                                break;
                            }

                        case 3:
                            {
                                var a = span[0];
                                var b = SafeBitConverterFake.ToUInt16(span.Slice(1));
                                key = a | (ulong)b << 8;
                                span = span.Slice(3);
                                break;
                            }

                        case 4:
                            {
                                key = SafeBitConverterFake.ToUInt32(span);
                                span = span.Slice(4);
                                break;
                            }

                        case 5:
                            {
                                var a = span[0];
                                var b = SafeBitConverterFake.ToUInt32(span.Slice(1));
                                key = a | (ulong)b << 8;
                                span = span.Slice(5);
                                break;
                            }

                        case 6:
                            {
                                ulong a = SafeBitConverterFake.ToUInt16(span);
                                ulong b = SafeBitConverterFake.ToUInt32(span.Slice(2));
                                key = a | (b << 16);
                                span = span.Slice(6);
                                break;
                            }

                        case 7:
                            {
                                var a = span[0];
                                var b = SafeBitConverterFake.ToUInt16(span.Slice(1));
                                var c = SafeBitConverterFake.ToUInt32(span.Slice(3));
                                key = a | (ulong)b << 8 | (ulong)c << 24;
                                span = span.Slice(7);
                                break;
                            }

                        default:
                            throw new Exception("Not Supported Length");
                    }
                }

                return key;
            }
        }
    }

    internal static class SafeBitConverterFake
    {
        internal static long ToInt64(ReadOnlySpan<byte> value)
        {
#if UNITY_ANDROID
            if (BitConverter.IsLittleEndian)
            {
                int i1 = value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24);
                int i2 = value[4] | (value[5] << 8) | (value[6] << 16) | (value[7] << 24);
                return (uint)i1 | ((long)i2 << 32);
            }
            else
            {
                int i1 = (value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3];
                int i2 = (value[4] << 24) | (value[5] << 16) | (value[6] << 8) | value[7];
                return (uint)i2 | ((long)i1 << 32);
            }
#else
            return MemoryMarshal.Cast<byte, long>(value)[0];
#endif
        }

        internal static ulong ToUInt64(ReadOnlySpan<byte> value) => unchecked((ulong)ToInt64(value));

        internal static ushort ToUInt16(ReadOnlySpan<byte> value)
        {
#if UNITY_ANDROID
            if (BitConverter.IsLittleEndian)
            {
                return (ushort)(value[0] | (value[1] << 8));
            }
            else
            {
                return (ushort)((value[0] << 8) | value[1]);
            }
#else
            return MemoryMarshal.Cast<byte, ushort>(value)[0];
#endif
        }

        internal static uint ToUInt32(ReadOnlySpan<byte> value)
        {
#if UNITY_ANDROID
            if (BitConverter.IsLittleEndian)
            {
                return (uint)(value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24));
            }
            else
            {
                return (uint)((value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3]);
            }
#else
            return MemoryMarshal.Cast<byte, uint>(value)[0];
#endif
        }
    }
}