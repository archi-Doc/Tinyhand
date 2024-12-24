// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Dynamic;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

public class ExpandoObjectFormatter : ITinyhandFormatter<ExpandoObject>
{
    public static readonly ITinyhandFormatter<ExpandoObject> Instance = new ExpandoObjectFormatter();

    private ExpandoObjectFormatter()
    {
    }

    public void Deserialize(ref TinyhandReader reader, ref ExpandoObject? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var result = new ExpandoObject();
        int count = reader.ReadMapHeader2();
        if (count > 0)
        {
            IFormatterResolver resolver = options.Resolver;
            ITinyhandFormatter<string> keyFormatter = resolver.GetFormatter<string>();
            ITinyhandFormatter<object> valueFormatter = resolver.GetFormatter<object>();
            IDictionary<string, object> dictionary = result!;

            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    string key = keyFormatter.Deserialize(ref reader, options) ?? string.Empty;
                    object v = valueFormatter.Deserialize(ref reader, options)!;
                    dictionary.Add(key, v);
                }
            }
            finally
            {
                reader.Depth--;
            }
        }

        value = result;
    }

    public void Serialize(ref TinyhandWriter writer, ExpandoObject? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            var dict = (IDictionary<string, object>)value!;
            var keyFormatter = options.Resolver.GetFormatter<string>();
            var valueFormatter = options.Resolver.GetFormatter<object>();

            writer.WriteMapHeader(dict.Count);
            foreach (var item in dict)
            {
                keyFormatter.Serialize(ref writer, item.Key, options);
                valueFormatter.Serialize(ref writer, item.Value, options);
            }
        }
    }

    public ExpandoObject Reconstruct(TinyhandSerializerOptions options)
    {
        return new ExpandoObject();
    }

    public ExpandoObject? Clone(ExpandoObject? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return null;
        }

        var result = new ExpandoObject();
        var dict = (IDictionary<string, object>)value!;

        if (dict.Count > 0)
        {
            var keyFormatter = options.Resolver.GetFormatter<string>();
            var valueFormatter = options.Resolver.GetFormatter<object>();

            foreach (var item in dict)
            {
                string k = keyFormatter.Clone(item.Key, options) ?? string.Empty;
                object v = valueFormatter.Clone(item.Value, options)!;
                dict.Add(k, v);
            }
        }

        return result;
    }
}
