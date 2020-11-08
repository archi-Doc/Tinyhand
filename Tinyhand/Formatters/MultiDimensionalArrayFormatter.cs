﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    /* multi dimensional array serialize to [i, j, [seq]] */

    public sealed class TwoDimensionalArrayFormatter<T> : ITinyhandFormatter<T[,]>
    {
        private const int ArrayLength = 3;

        public void Serialize(ref TinyhandWriter writer, T[,]? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);

                var formatter = options.Resolver.GetFormatter<T>();

                writer.WriteArrayHeader(ArrayLength);
                writer.Write(i);
                writer.Write(j);

                writer.WriteArrayHeader(value.Length);
                foreach (T item in value)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    formatter.Serialize(ref writer, item, options);
                }
            }
        }

        public T[,]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var formatter = options.Resolver.GetFormatter<T>();

                var len = reader.ReadArrayHeader();
                if (len != ArrayLength)
                {
                    throw new TinyhandException("Invalid T[,] format");
                }

                var iLength = reader.ReadInt32();
                var jLength = reader.ReadInt32();
                var maxLen = reader.ReadArrayHeader();

                var array = new T[iLength, jLength];

                var i = 0;
                var j = -1;
                options.Security.DepthStep(ref reader);
                try
                {
                    for (int loop = 0; loop < maxLen; loop++)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();
                        if (j < jLength - 1)
                        {
                            j++;
                        }
                        else
                        {
                            j = 0;
                            i++;
                        }

                        array[i, j] = formatter.Deserialize(ref reader, options) !; // ?? formatter.Reconstruct(options);
                    }
                }
                finally
                {
                    reader.Depth--;
                }

                return array;
            }
        }

        public T[,] Reconstruct(TinyhandSerializerOptions options) => new T[0, 0];
    }

    public sealed class ThreeDimensionalArrayFormatter<T> : ITinyhandFormatter<T[,,]>
    {
        private const int ArrayLength = 4;

        public void Serialize(ref TinyhandWriter writer, T[,,]? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);
                var k = value.GetLength(2);

                var formatter = options.Resolver.GetFormatter<T>();

                writer.WriteArrayHeader(ArrayLength);
                writer.Write(i);
                writer.Write(j);
                writer.Write(k);

                writer.WriteArrayHeader(value.Length);
                foreach (T item in value)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    formatter.Serialize(ref writer, item, options);
                }
            }
        }

        public T[,,]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var formatter = options.Resolver.GetFormatter<T>();

                var len = reader.ReadArrayHeader();
                if (len != ArrayLength)
                {
                    throw new TinyhandException("Invalid T[,,] format");
                }

                var iLength = reader.ReadInt32();
                var jLength = reader.ReadInt32();
                var kLength = reader.ReadInt32();
                var maxLen = reader.ReadArrayHeader();

                var array = new T[iLength, jLength, kLength];

                var i = 0;
                var j = 0;
                var k = -1;
                options.Security.DepthStep(ref reader);
                try
                {
                    for (int loop = 0; loop < maxLen; loop++)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();
                        if (k < kLength - 1)
                        {
                            k++;
                        }
                        else if (j < jLength - 1)
                        {
                            k = 0;
                            j++;
                        }
                        else
                        {
                            k = 0;
                            j = 0;
                            i++;
                        }

                        array[i, j, k] = formatter.Deserialize(ref reader, options) !; // ?? formatter.Reconstruct(options);
                    }
                }
                finally
                {
                    reader.Depth--;
                }

                return array;
            }
        }

        public T[,,] Reconstruct(TinyhandSerializerOptions options) => new T[0, 0, 0];
    }

    public sealed class FourDimensionalArrayFormatter<T> : ITinyhandFormatter<T[,,,]>
    {
        private const int ArrayLength = 5;

        public void Serialize(ref TinyhandWriter writer, T[,,,]? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);
                var k = value.GetLength(2);
                var l = value.GetLength(3);

                var formatter = options.Resolver.GetFormatter<T>();

                writer.WriteArrayHeader(ArrayLength);
                writer.Write(i);
                writer.Write(j);
                writer.Write(k);
                writer.Write(l);

                writer.WriteArrayHeader(value.Length);
                foreach (T item in value)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    formatter.Serialize(ref writer, item, options);
                }
            }
        }

        public T[,,,]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var formatter = options.Resolver.GetFormatter<T>();

                var len = reader.ReadArrayHeader();
                if (len != ArrayLength)
                {
                    throw new TinyhandException("Invalid T[,,,] format");
                }

                var iLength = reader.ReadInt32();
                var jLength = reader.ReadInt32();
                var kLength = reader.ReadInt32();
                var lLength = reader.ReadInt32();
                var maxLen = reader.ReadArrayHeader();
                var array = new T[iLength, jLength, kLength, lLength];

                var i = 0;
                var j = 0;
                var k = 0;
                var l = -1;
                options.Security.DepthStep(ref reader);
                try
                {
                    for (int loop = 0; loop < maxLen; loop++)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();
                        if (l < lLength - 1)
                        {
                            l++;
                        }
                        else if (k < kLength - 1)
                        {
                            l = 0;
                            k++;
                        }
                        else if (j < jLength - 1)
                        {
                            l = 0;
                            k = 0;
                            j++;
                        }
                        else
                        {
                            l = 0;
                            k = 0;
                            j = 0;
                            i++;
                        }

                        array[i, j, k, l] = formatter.Deserialize(ref reader, options) !; // ?? formatter.Reconstruct(options);
                    }
                }
                finally
                {
                    reader.Depth--;
                }

                return array;
            }
        }

        public T[,,,] Reconstruct(TinyhandSerializerOptions options) => new T[0, 0, 0, 0];
    }
}
