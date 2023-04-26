// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Tinyhand.Generator;

public class TinyhandUnion
{
    public static TinyhandUnion? CreateFromObject(TinyhandObject obj)
    {
        List<TinyhandUnionAttributeMock>? unionList = null;
        var errorFlag = false;
        foreach (var x in obj.AllAttributes)
        {
            if (x.FullName == TinyhandUnionAttributeMock.FullName)
            {
                TinyhandUnionAttributeMock attr;
                try
                {
                    attr = TinyhandUnionAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments, x.Location);
                }
                catch (InvalidCastException)
                {
                    obj.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                    errorFlag = true;
                    continue;
                }

                if (attr.SubType == null)
                {
                    obj.Body.ReportDiagnostic(TinyhandBody.Error_NullSubtype, x.Location);
                    errorFlag = true;
                    continue;
                }

                if (unionList == null)
                {
                    unionList = new();
                }

                unionList.Add(attr);
            }
        }

        if (errorFlag)
        {
            return null;
        }

        if (unionList == null)
        {// No union attribute.
            return null;
        }

        if (!obj.IsAbstractOrInterface)
        {// Union can only be interface or abstract class.
            obj.Body.ReportDiagnostic(TinyhandBody.Error_UnionType, obj.Location);
            return null;
        }

        // Check for duplicates.
        var checker1 = new HashSet<int>();
        var checker2 = new HashSet<ISymbol?>();
        var checker3 = new HashSet<string>();
        foreach (var item in unionList)
        {
            if (item.HasStringKey)
            {// String key
                if (!checker3.Add(item.StringKey!))
                {
                    obj.Body.ReportDiagnostic(TinyhandBody.Error_StringKeyConflict, item.Location);
                    errorFlag = true;
                }
            }
            else
            {// Int key
                if (checker3.Count > 0)
                {// Integer and string keys are exclusive
                    obj.Body.ReportDiagnostic(TinyhandBody.Error_IntStringKeyConflict, item.Location);
                    errorFlag = true;
                }
                else if (!checker1.Add(item.IntKey))
                {
                    obj.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyConflicted, item.Location);
                    errorFlag = true;
                }
            }

            if (!checker2.Add(item.SubType))
            {
                obj.Body.ReportDiagnostic(TinyhandBody.Error_SubtypeConflicted, item.Location);
                errorFlag = true;
            }
        }

        if (errorFlag)
        {
            return null;
        }

        return new TinyhandUnion(obj, unionList);
    }

    public TinyhandUnion(TinyhandObject obj, List<TinyhandUnionAttributeMock> unionList)
    {
        this.Object = obj;
        this.UnionList = unionList;
    }

    public TinyhandObject Object { get; }

    public List<TinyhandUnionAttributeMock> UnionList { get; }

    // public SortedDictionary<int, TinyhandObject>? IntDictionary { get; private set; }

    public bool HasStringKey { get; private set; }

    public SortedDictionary<string, TinyhandObject>? StringDictionary { get; private set; }

    public string DelegateIdentifier { get; private set; } = string.Empty;

    public string TableIdentifier { get; private set; } = string.Empty;

    // internal VisceralTrieInt<TinyhandObject, TinyhandObject>? TrieInt { get; private set; }

    public void CheckAndPrepare()
    {
        // Create SortedDictionary
        var errorFlag = false;
        foreach (var x in this.UnionList)
        {
            if (x.SubType is INamedTypeSymbol nts &&
                this.Object.Body.TryGet(nts, out var obj) &&
                this.Object.Body.TryGet(nts.ConstructedFrom, out var obj2) &&
                obj2.ObjectAttribute != null)
            {
                if (obj == this.Object)
                {
                    this.Object.Body.ReportDiagnostic(TinyhandBody.Error_UnionSelf, x.Location, obj.FullName);
                    errorFlag = true;
                }
                else if (obj.IsDerivedOrImplementing(this.Object))
                {// Success
                    if (x.HasStringKey)
                    {
                        this.HasStringKey = true;
                        this.StringDictionary ??= new();
                        this.StringDictionary.Add($"\"{x.StringKey!}\"", obj);
                    }
                    else
                    {
                        this.StringDictionary ??= new();
                        this.StringDictionary.Add(x.IntKey.ToString(), obj);
                    }
                }
                else if (this.Object.Kind == Arc.Visceral.VisceralObjectKind.Interface)
                {
                    this.Object.Body.ReportDiagnostic(TinyhandBody.Error_UnionNotImplementing, x.Location, obj.FullName, this.Object.FullName);
                    errorFlag = true;
                }
                else
                {
                    this.Object.Body.ReportDiagnostic(TinyhandBody.Error_UnionNotDerived, x.Location, obj.FullName, this.Object.FullName);
                    errorFlag = true;
                }
            }
            else
            {
                this.Object.Body.ReportDiagnostic(TinyhandBody.Error_UnionTargetError, x.Location);
                errorFlag = true;
            }
        }

        if (errorFlag)
        {
            return;
        }

        /*this.TrieInt ??= new(this.Object);
        foreach (var x in this.UnionDictionary)
        {
            this.TrieInt.AddNode(x.Key, x.Value);
        }*/
    }

    /*internal void GenerateFormatter_Serialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {// switch
        if (this.UnionDictionary == null)
        {
            return;
        }

        ssb.AppendLine($"if ({ssb.FullObject} == null) {{ writer.WriteNil(); return; }}");

        ssb.AppendLine("writer.WriteArrayHeader(2);");
        using (var sw = ssb.ScopeBrace($"switch ({ssb.FullObject})"))
        {
            foreach (var x in this.UnionDictionary)
            {
                var keyString = x.Key.ToString();
                var name = "x" + keyString;
                ssb.AppendLine($"case {x.Value.FullName} {name}:");
                ssb.IncrementIndent();
                ssb.AppendLine("writer.Write(" + keyString + ");");
                ssb.AppendLine($"options.Resolver.GetFormatter<{x.Value.FullName}>().Serialize(ref writer, {name}, options);");
                ssb.AppendLine("break;");
                ssb.DecrementIndent();
            }

            ssb.AppendLine("default:");
            ssb.IncrementIndent();
            ssb.AppendLine("writer.WriteNil();");
            ssb.AppendLine("writer.WriteNil();");
            ssb.AppendLine("break;");
            ssb.DecrementIndent();
        }
    }*/

    internal void GenerateTable(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.StringDictionary == null)
        {
            return;
        }

        // Identifier
        this.DelegateIdentifier = this.Object.Identifier.GetIdentifier();
        this.TableIdentifier = this.Object.Identifier.GetIdentifier();
        var initializeMethod = this.Object.Identifier.GetIdentifier();

        // Prepare
        var interfaceName = this.Object.RegionalName + this.Object.QuestionMarkIfReferenceType;

        // Delegate
        ssb.AppendLine($"private delegate void {this.DelegateIdentifier}(ref TinyhandWriter writer, ref {interfaceName} v, TinyhandSerializerOptions options);");

        // Table
        ssb.AppendLine($"private static ThreadsafeTypeKeyHashTable<{this.DelegateIdentifier}> {this.TableIdentifier} = {initializeMethod}();");

        // initializeMethod
        using (var scopeMethod = ssb.ScopeBrace($"private static ThreadsafeTypeKeyHashTable<{this.DelegateIdentifier}> {initializeMethod}()"))
        {
            ssb.AppendLine($"var table = new ThreadsafeTypeKeyHashTable<{this.DelegateIdentifier}>();");
            foreach (var x in this.StringDictionary)
            {
                ssb.AppendLine($"table.TryAdd(typeof({x.Value.FullName}), static (ref TinyhandWriter writer, ref {interfaceName} v, TinyhandSerializerOptions options) =>");
                ssb.AppendLine("{");
                ssb.IncrementIndent();

                ssb.AppendLine("writer.Write(" + x.Key + ");");
                ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, Unsafe.As<{x.Value.FullName}>(v), options);");

                ssb.DecrementIndent();
                ssb.AppendLine("});");
            }

            ssb.AppendLine("return table;");
        }

        ssb.AppendLine();
    }

    internal void GenerateFormatter_Serialize2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.StringDictionary == null)
        {
            return;
        }

        ssb.AppendLine($"if ({ssb.FullObject} == null) {{ writer.WriteNil(); return; }}");
        ssb.AppendLine("writer.WriteMapHeader(1);"); // WriteArrayHeader(2) -> WriteMapHeader(1)
        ssb.AppendLine($"var type = {ssb.FullObject}.GetType();");

        using (ssb.ScopeBrace($"if ({this.TableIdentifier}.TryGetValue(type, out var func))"))
        {
            ssb.AppendLine($"func(ref writer, ref {ssb.FullObject}, options);");
        }

        using (ssb.ScopeBrace("else"))
        {
            ssb.AppendLine("writer.WriteNil();");
            ssb.AppendLine("writer.WriteNil();");
        }

        // Obsolete
        /*var firstFlag = true;
        foreach (var x in this.StringDictionary)
        {
            string t;
            if (firstFlag)
            {
                firstFlag = false;
                t = $"if (type == typeof({x.Value.FullName}))";
            }
            else
            {
                t = $"else if (type == typeof({x.Value.FullName}))";
            }

            using (ssb.ScopeBrace(t))
            {
                ssb.AppendLine("writer.Write(" + x.Key + ");");
                ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, Unsafe.As<{x.Value.FullName}>({ssb.FullObject}), options);");
            }
        }

        using (ssb.ScopeBrace("else"))
        {
            ssb.AppendLine("writer.WriteNil();");
            ssb.AppendLine("writer.WriteNil();");
        }*/
    }

    internal void GenerateFormatter_Deserialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.StringDictionary == null/* || this.TrieInt == null*/)
        {
            return;
        }

        ssb.AppendLine($"if (reader.TryReadNil()) {{ return; }}");
        ssb.AppendLine("if (reader.ReadMapHeader() != 1) { throw new TinyhandException(\"Invalid Union data was detected.\"); }"); // reader.ReadArrayHeader() != 2 -> reader.ReadMapHeader() != 1
        ssb.AppendLine($"if (reader.TryReadNil()) {{ reader.ReadNil(); return; }}");

        /*var context = new VisceralTrieInt<TinyhandObject, TinyhandObject>.VisceralTrieContext(
            static (context, obj, node) =>
            {
                var name = "x" + node.Index;
                context.Ssb.AppendLine($"var {name} = Unsafe.As<{node.Member.FullName + node.Member.QuestionMarkIfReferenceType}>({context.Ssb.FullObject});");
                context.Ssb.AppendLine($"TinyhandSerializer.DeserializeObject(ref reader, ref {name}, options);");
                context.Ssb.AppendLine($"{context.Ssb.FullObject} = Unsafe.As<{obj.FullName + obj.QuestionMarkIfReferenceType}>({name});");
                context.Ssb.AppendLine("return;");
            },
            ssb,
            null);
        context.AddContinueStatement = false;

        this.TrieInt.Generate(context);*/

        if (this.HasStringKey)
        {
            ssb.AppendLine("var key = reader.ReadString();");
        }
        else
        {
            ssb.AppendLine("var key = reader.ReadInt32();");
        }

        using (var sw = ssb.ScopeBrace("switch (key)"))
        {
            var n = 0;
            foreach (var x in this.StringDictionary)
            {
                var keyString = x.Key;
                var name = "x" + n++;
                ssb.AppendLine("case " + keyString + ":");
                ssb.IncrementIndent();

                // ssb.AppendLine($"var {name} = Unsafe.As<{x.Value.FullName + x.Value.QuestionMarkIfReferenceType}>({ssb.FullObject});");
                ssb.AppendLine($"var {name} = {ssb.FullObject} as {x.Value.FullName};");
                ssb.AppendLine($"TinyhandSerializer.DeserializeObject(ref reader, ref {name}, options);");
                ssb.AppendLine($"{ssb.FullObject} = Unsafe.As<{this.Object.FullName + this.Object.QuestionMarkIfReferenceType}>({name});");
                ssb.AppendLine("return;");

                ssb.DecrementIndent();
            }

            ssb.AppendLine("default:");
            ssb.IncrementIndent();
            ssb.AppendLine("reader.Skip();");
            ssb.AppendLine("return;");
            ssb.DecrementIndent();
        }
    }
}
