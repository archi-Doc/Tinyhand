// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;

namespace Tinyhand.IO
{
    public ref partial struct TinyhandReader
    {
        /// <summary>
        /// Reads an <see cref="byte"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public byte ReadUInt8()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((byte)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((byte)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((byte)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((byte)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((byte)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((byte)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((byte)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((byte)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((byte)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (byte)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }

        /// <summary>
        /// Reads an <see cref="sbyte"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public sbyte ReadInt8()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((sbyte)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((sbyte)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((sbyte)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((sbyte)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((sbyte)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((sbyte)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((sbyte)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((sbyte)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((sbyte)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (sbyte)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }

        /// <summary>
        /// Reads an <see cref="ushort"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public ushort ReadUInt16()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((ushort)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((ushort)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((ushort)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((ushort)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((ushort)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((ushort)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((ushort)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((ushort)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((ushort)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (ushort)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }

        /// <summary>
        /// Reads an <see cref="short"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public short ReadInt16()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((short)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((short)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((short)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((short)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((short)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((short)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((short)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((short)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((short)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (short)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }

        /// <summary>
        /// Reads an <see cref="uint"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public uint ReadUInt32()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((uint)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((uint)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((uint)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((uint)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((uint)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((uint)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((uint)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((uint)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((uint)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (uint)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }

        /// <summary>
        /// Reads an <see cref="int"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public int ReadInt32()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((int)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((int)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((int)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((int)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((int)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((int)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((int)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((int)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((int)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (int)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }

        /// <summary>
        /// Reads an <see cref="ulong"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public ulong ReadUInt64()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((ulong)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((ulong)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((ulong)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((ulong)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((ulong)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((ulong)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((ulong)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((ulong)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((ulong)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (ulong)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }

        /// <summary>
        /// Reads an <see cref="long"/> value from:
        /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
        /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
        /// or any of the other MsgPack integer types.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
        public long ReadInt64()
        {
            ThrowInsufficientBufferUnless(this.TryRead(out byte code));

            switch (code)
            {
                case MessagePackCode.UInt8:
                    ThrowInsufficientBufferUnless(this.TryRead(out byte byteResult));
                    return checked((long)byteResult);
                case MessagePackCode.Int8:
                    ThrowInsufficientBufferUnless(this.TryRead(out sbyte sbyteResult));
                    return checked((long)sbyteResult);
                case MessagePackCode.UInt16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ushort ushortResult));
                    return checked((long)ushortResult);
                case MessagePackCode.Int16:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out short shortResult));
                    return checked((long)shortResult);
                case MessagePackCode.UInt32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out uint uintResult));
                    return checked((long)uintResult);
                case MessagePackCode.Int32:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out int intResult));
                    return checked((long)intResult);
                case MessagePackCode.UInt64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out ulong ulongResult));
                    return checked((long)ulongResult);
                case MessagePackCode.Int64:
                    ThrowInsufficientBufferUnless(this.TryReadBigEndian(out long longResult));
                    return checked((long)longResult);
                default:
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        return checked((long)unchecked((sbyte)code));
                    }

                    if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        return (long)code;
                    }

                    throw ThrowInvalidCode(code, MessagePackType.Integer);
            }
        }
    }
}
