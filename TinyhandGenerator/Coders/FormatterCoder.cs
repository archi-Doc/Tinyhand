// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class FormatterResolver : ICoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly FormatterResolver Instance = new();

        public FormatterResolver()
        {
            this.AddFormatter("decimal", false);
        }

        public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
        {
            this.stringToCoder.TryGetValue(withNullable.FullNameWithNullable, out var value);
            return value;
        }

        private Dictionary<string, ITinyhandCoder> stringToCoder = new();
    }

    private class FormatterCoder : ITinyhandCoder
    {
        public FormatterCoder(string fullName, )
        {
            this.name = name;
        }

        public string FullName { get; }

        public bool Nullable { get; }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"options.Resolver.GetFormatter<{this.FullName}>().Serialize(ref writer, {ssb.FullObject}, options);");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (this.Nullable)
            {
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullName}>().Deserialize(ref reader, options);");
            }
            else
            {
                ssb.AppendLine($"{ssb.FullObject} = options.ResolveAndDeserializeReconstruct<{this.FullName}>(ref reader);");
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullName}>().Reconstruct(options);");
        }

        private WithNullable<TinyhandObject> withNullable;
    }
}
