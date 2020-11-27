// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Buffers;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    public sealed class ValueTupleFormatter<T1> : ITinyhandFormatter<ValueTuple<T1>>
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(1);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 1)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);

                    value = new ValueTuple<T1>(item1!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1>(default!);
        }
    }

    public sealed class ValueTupleFormatter<T1, T2> : ITinyhandFormatter<ValueTuple<T1, T2>>
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1, T2> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(2);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
            resolver.GetFormatter<T2>().Serialize(ref writer, ref value.Item2!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1, T2> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 2)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);
                    T2 item2 = default;
                    resolver.GetFormatter<T2>().Deserialize(ref reader, ref item2, options);

                    value = new ValueTuple<T1, T2>(item1!, item2!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1, T2> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1, T2>(default!, default!);
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3> : ITinyhandFormatter<ValueTuple<T1, T2, T3>>
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1, T2, T3> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(3);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
            resolver.GetFormatter<T2>().Serialize(ref writer, ref value.Item2!, options);
            resolver.GetFormatter<T3>().Serialize(ref writer, ref value.Item3!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1, T2, T3> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 3)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);
                    T2 item2 = default;
                    resolver.GetFormatter<T2>().Deserialize(ref reader, ref item2, options);
                    T3 item3 = default;
                    resolver.GetFormatter<T3>().Deserialize(ref reader, ref item3, options);

                    value = new ValueTuple<T1, T2, T3>(item1!, item2!, item3!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1, T2, T3> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1, T2, T3>(default!, default!, default!);
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4> : ITinyhandFormatter<ValueTuple<T1, T2, T3, T4>>
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1, T2, T3, T4> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(4);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
            resolver.GetFormatter<T2>().Serialize(ref writer, ref value.Item2!, options);
            resolver.GetFormatter<T3>().Serialize(ref writer, ref value.Item3!, options);
            resolver.GetFormatter<T4>().Serialize(ref writer, ref value.Item4!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1, T2, T3, T4> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 4)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);
                    T2 item2 = default;
                    resolver.GetFormatter<T2>().Deserialize(ref reader, ref item2, options);
                    T3 item3 = default;
                    resolver.GetFormatter<T3>().Deserialize(ref reader, ref item3, options);
                    T4 item4 = default;
                    resolver.GetFormatter<T4>().Deserialize(ref reader, ref item4, options);

                    value = new ValueTuple<T1, T2, T3, T4>(item1!, item2!, item3!, item4!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1, T2, T3, T4> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1, T2, T3, T4>(default!, default!, default!, default!);
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5> : ITinyhandFormatter<ValueTuple<T1, T2, T3, T4, T5>>
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1, T2, T3, T4, T5> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(5);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
            resolver.GetFormatter<T2>().Serialize(ref writer, ref value.Item2!, options);
            resolver.GetFormatter<T3>().Serialize(ref writer, ref value.Item3!, options);
            resolver.GetFormatter<T4>().Serialize(ref writer, ref value.Item4!, options);
            resolver.GetFormatter<T5>().Serialize(ref writer, ref value.Item5!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1, T2, T3, T4, T5> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 5)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);
                    T2 item2 = default;
                    resolver.GetFormatter<T2>().Deserialize(ref reader, ref item2, options);
                    T3 item3 = default;
                    resolver.GetFormatter<T3>().Deserialize(ref reader, ref item3, options);
                    T4 item4 = default;
                    resolver.GetFormatter<T4>().Deserialize(ref reader, ref item4, options);
                    T5 item5 = default;
                    resolver.GetFormatter<T5>().Deserialize(ref reader, ref item5, options);

                    value = new ValueTuple<T1, T2, T3, T4, T5>(item1!, item2!, item3!, item4!, item5!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1, T2, T3, T4, T5> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1, T2, T3, T4, T5>(default!, default!, default!, default!, default!);
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6> : ITinyhandFormatter<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1, T2, T3, T4, T5, T6> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(6);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
            resolver.GetFormatter<T2>().Serialize(ref writer, ref value.Item2!, options);
            resolver.GetFormatter<T3>().Serialize(ref writer, ref value.Item3!, options);
            resolver.GetFormatter<T4>().Serialize(ref writer, ref value.Item4!, options);
            resolver.GetFormatter<T5>().Serialize(ref writer, ref value.Item5!, options);
            resolver.GetFormatter<T6>().Serialize(ref writer, ref value.Item6!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1, T2, T3, T4, T5, T6> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 6)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);
                    T2 item2 = default;
                    resolver.GetFormatter<T2>().Deserialize(ref reader, ref item2, options);
                    T3 item3 = default;
                    resolver.GetFormatter<T3>().Deserialize(ref reader, ref item3, options);
                    T4 item4 = default;
                    resolver.GetFormatter<T4>().Deserialize(ref reader, ref item4, options);
                    T5 item5 = default;
                    resolver.GetFormatter<T5>().Deserialize(ref reader, ref item5, options);
                    T6 item6 = default;
                    resolver.GetFormatter<T6>().Deserialize(ref reader, ref item6, options);

                    value = new ValueTuple<T1, T2, T3, T4, T5, T6>(item1!, item2!, item3!, item4!, item5!, item6!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1, T2, T3, T4, T5, T6> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6>(default!, default!, default!, default!, default!, default!);
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7> : ITinyhandFormatter<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1, T2, T3, T4, T5, T6, T7> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(7);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
            resolver.GetFormatter<T2>().Serialize(ref writer, ref value.Item2!, options);
            resolver.GetFormatter<T3>().Serialize(ref writer, ref value.Item3!, options);
            resolver.GetFormatter<T4>().Serialize(ref writer, ref value.Item4!, options);
            resolver.GetFormatter<T5>().Serialize(ref writer, ref value.Item5!, options);
            resolver.GetFormatter<T6>().Serialize(ref writer, ref value.Item6!, options);
            resolver.GetFormatter<T7>().Serialize(ref writer, ref value.Item7!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1, T2, T3, T4, T5, T6, T7> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 7)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);
                    T2 item2 = default;
                    resolver.GetFormatter<T2>().Deserialize(ref reader, ref item2, options);
                    T3 item3 = default;
                    resolver.GetFormatter<T3>().Deserialize(ref reader, ref item3, options);
                    T4 item4 = default;
                    resolver.GetFormatter<T4>().Deserialize(ref reader, ref item4, options);
                    T5 item5 = default;
                    resolver.GetFormatter<T5>().Deserialize(ref reader, ref item5, options);
                    T6 item6 = default;
                    resolver.GetFormatter<T6>().Deserialize(ref reader, ref item6, options);
                    T7 item7 = default;
                    resolver.GetFormatter<T7>().Deserialize(ref reader, ref item7, options);

                    value = new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(item1!, item2!, item3!, item4!, item5!, item6!, item7!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(default!, default!, default!, default!, default!, default!, default!);
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7, TRest> : ITinyhandFormatter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
        where TRest : struct
    {
        public void Serialize(ref TinyhandWriter writer, ref ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(8);

            var resolver = options.Resolver;
            resolver.GetFormatter<T1>().Serialize(ref writer, ref value.Item1!, options);
            resolver.GetFormatter<T2>().Serialize(ref writer, ref value.Item2!, options);
            resolver.GetFormatter<T3>().Serialize(ref writer, ref value.Item3!, options);
            resolver.GetFormatter<T4>().Serialize(ref writer, ref value.Item4!, options);
            resolver.GetFormatter<T5>().Serialize(ref writer, ref value.Item5!, options);
            resolver.GetFormatter<T6>().Serialize(ref writer, ref value.Item6!, options);
            resolver.GetFormatter<T7>().Serialize(ref writer, ref value.Item7!, options);
            resolver.GetFormatter<TRest>().Serialize(ref writer, ref value.Rest!, options);
        }

        public void Deserialize(ref TinyhandReader reader, ref ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new TinyhandException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 8)
                {
                    throw new TinyhandException("Invalid ValueTuple count");
                }

                var resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = default;
                    resolver.GetFormatter<T1>().Deserialize(ref reader, ref item1, options);
                    T2 item2 = default;
                    resolver.GetFormatter<T2>().Deserialize(ref reader, ref item2, options);
                    T3 item3 = default;
                    resolver.GetFormatter<T3>().Deserialize(ref reader, ref item3, options);
                    T4 item4 = default;
                    resolver.GetFormatter<T4>().Deserialize(ref reader, ref item4, options);
                    T5 item5 = default;
                    resolver.GetFormatter<T5>().Deserialize(ref reader, ref item5, options);
                    T6 item6 = default;
                    resolver.GetFormatter<T6>().Deserialize(ref reader, ref item6, options);
                    T7 item7 = default;
                    resolver.GetFormatter<T7>().Deserialize(ref reader, ref item7, options);
                    TRest item8 = default;
                    resolver.GetFormatter<TRest>().Deserialize(ref reader, ref item8, options);

                    value = new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1!, item2!, item3!, item4!, item5!, item6!, item7!, item8!);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> Reconstruct(TinyhandSerializerOptions options)
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(default!, default!, default!, default!, default!, default!, default!, default!);
        }
    }
}
