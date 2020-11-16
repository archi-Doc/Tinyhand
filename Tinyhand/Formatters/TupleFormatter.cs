// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Buffers;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    public sealed class TupleFormatter<T1> : ITinyhandFormatter<Tuple<T1>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(1);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
            }
        }

        public Tuple<T1>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 1)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);

                    return new Tuple<T1>(item1!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1>(default!);
        }
    }

    public sealed class TupleFormatter<T1, T2> : ITinyhandFormatter<Tuple<T1, T2>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1, T2>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(2);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
                resolver.GetFormatter<T2>().Serialize(ref writer, value.Item2, options);
            }
        }

        public Tuple<T1, T2>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 2)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatter<T2>().Deserialize(ref reader, options);

                    return new Tuple<T1, T2>(item1!, item2!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1, T2> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1, T2>(default!, default!);
        }
    }

    public sealed class TupleFormatter<T1, T2, T3> : ITinyhandFormatter<Tuple<T1, T2, T3>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1, T2, T3>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(3);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
                resolver.GetFormatter<T2>().Serialize(ref writer, value.Item2, options);
                resolver.GetFormatter<T3>().Serialize(ref writer, value.Item3, options);
            }
        }

        public Tuple<T1, T2, T3>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 3)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatter<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatter<T3>().Deserialize(ref reader, options);

                    return new Tuple<T1, T2, T3>(item1!, item2!, item3!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1, T2, T3> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1, T2, T3>(default!, default!, default!);
        }
    }

    public sealed class TupleFormatter<T1, T2, T3, T4> : ITinyhandFormatter<Tuple<T1, T2, T3, T4>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1, T2, T3, T4>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(4);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
                resolver.GetFormatter<T2>().Serialize(ref writer, value.Item2, options);
                resolver.GetFormatter<T3>().Serialize(ref writer, value.Item3, options);
                resolver.GetFormatter<T4>().Serialize(ref writer, value.Item4, options);
            }
        }

        public Tuple<T1, T2, T3, T4>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 4)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatter<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatter<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatter<T4>().Deserialize(ref reader, options);

                    return new Tuple<T1, T2, T3, T4>(item1!, item2!, item3!, item4!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1, T2, T3, T4> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1, T2, T3, T4>(default!, default!, default!, default!);
        }
    }

    public sealed class TupleFormatter<T1, T2, T3, T4, T5> : ITinyhandFormatter<Tuple<T1, T2, T3, T4, T5>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1, T2, T3, T4, T5>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(5);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
                resolver.GetFormatter<T2>().Serialize(ref writer, value.Item2, options);
                resolver.GetFormatter<T3>().Serialize(ref writer, value.Item3, options);
                resolver.GetFormatter<T4>().Serialize(ref writer, value.Item4, options);
                resolver.GetFormatter<T5>().Serialize(ref writer, value.Item5, options);
            }
        }

        public Tuple<T1, T2, T3, T4, T5>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 5)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatter<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatter<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatter<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatter<T5>().Deserialize(ref reader, options);

                    return new Tuple<T1, T2, T3, T4, T5>(item1!, item2!, item3!, item4!, item5!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1, T2, T3, T4, T5> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1, T2, T3, T4, T5>(default!, default!, default!, default!, default!);
        }
    }

    public sealed class TupleFormatter<T1, T2, T3, T4, T5, T6> : ITinyhandFormatter<Tuple<T1, T2, T3, T4, T5, T6>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1, T2, T3, T4, T5, T6>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(6);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
                resolver.GetFormatter<T2>().Serialize(ref writer, value.Item2, options);
                resolver.GetFormatter<T3>().Serialize(ref writer, value.Item3, options);
                resolver.GetFormatter<T4>().Serialize(ref writer, value.Item4, options);
                resolver.GetFormatter<T5>().Serialize(ref writer, value.Item5, options);
                resolver.GetFormatter<T6>().Serialize(ref writer, value.Item6, options);
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 6)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatter<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatter<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatter<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatter<T5>().Deserialize(ref reader, options);
                    T6 item6 = resolver.GetFormatter<T6>().Deserialize(ref reader, options);

                    return new Tuple<T1, T2, T3, T4, T5, T6>(item1!, item2!, item3!, item4!, item5!, item6!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6>(default!, default!, default!, default!, default!, default!);
        }
    }

    public sealed class TupleFormatter<T1, T2, T3, T4, T5, T6, T7> : ITinyhandFormatter<Tuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(7);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
                resolver.GetFormatter<T2>().Serialize(ref writer, value.Item2, options);
                resolver.GetFormatter<T3>().Serialize(ref writer, value.Item3, options);
                resolver.GetFormatter<T4>().Serialize(ref writer, value.Item4, options);
                resolver.GetFormatter<T5>().Serialize(ref writer, value.Item5, options);
                resolver.GetFormatter<T6>().Serialize(ref writer, value.Item6, options);
                resolver.GetFormatter<T7>().Serialize(ref writer, value.Item7, options);
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 7)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatter<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatter<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatter<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatter<T5>().Deserialize(ref reader, options);
                    T6 item6 = resolver.GetFormatter<T6>().Deserialize(ref reader, options);
                    T7 item7 = resolver.GetFormatter<T7>().Deserialize(ref reader, options);

                    return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1!, item2!, item3!, item4!, item5!, item6!, item7!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(default!, default!, default!, default!, default!, default!, default!);
        }
    }

    public sealed class TupleFormatter<T1, T2, T3, T4, T5, T6, T7, TRest> : ITinyhandFormatter<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
    {
        public void Serialize(ref TinyhandWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(8);

                var resolver = options.Resolver;
                resolver.GetFormatter<T1>().Serialize(ref writer, value.Item1, options);
                resolver.GetFormatter<T2>().Serialize(ref writer, value.Item2, options);
                resolver.GetFormatter<T3>().Serialize(ref writer, value.Item3, options);
                resolver.GetFormatter<T4>().Serialize(ref writer, value.Item4, options);
                resolver.GetFormatter<T5>().Serialize(ref writer, value.Item5, options);
                resolver.GetFormatter<T6>().Serialize(ref writer, value.Item6, options);
                resolver.GetFormatter<T7>().Serialize(ref writer, value.Item7, options);
                resolver.GetFormatter<TRest>().Serialize(ref writer, value.Rest, options);
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 8)
                {
                    throw new TinyhandException("Invalid Tuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatter<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatter<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatter<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatter<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatter<T5>().Deserialize(ref reader, options);
                    T6 item6 = resolver.GetFormatter<T6>().Deserialize(ref reader, options);
                    T7 item7 = resolver.GetFormatter<T7>().Deserialize(ref reader, options);
                    TRest item8 = resolver.GetFormatter<TRest>().Deserialize(ref reader, options);

                    return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1!, item2!, item3!, item4!, item5!, item6!, item7!, item8!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(default!, default!, default!, default!, default!, default!, default!, default!);
        }
    }
}
