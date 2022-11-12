// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Tinyhand.Generator;

/*public class UnionToItem
{
    public UnionToItem(TinyhandUnionToAttributeMock unionTo, TinyhandObject baseObject)
    {
        this.UnionTo = unionTo;
        this.BaseObject = baseObject;
    }

    public TinyhandUnionToAttributeMock UnionTo { get; set; }

    public TinyhandObject BaseObject { get; set; }
}*/

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
        foreach (var item in unionList)
        {
            if (!checker1.Add(item.Key))
            {
                obj.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyConflicted, item.Location);
                errorFlag = true;
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

    /*public static void ProcessUnionTo(UnionToItem item)
    {
        var body = item.BaseObject.Body;
        if (item.UnionTo.SubType == null || !body.TryGet(item.UnionTo.SubType, out var subObject))
        {
            return;
        }

        if (item.BaseObject.ObjectAttribute == null || !item.BaseObject.IsAbstractOrInterface)
        {
            body.AddDiagnostic(TinyhandBody.Error_UnionToError, item.UnionTo.Location);
            return;
        }

        if (!subObject.IsDerivedOrImplementing(item.BaseObject))
        {
            if (item.BaseObject.Kind == Arc.Visceral.VisceralObjectKind.Interface)
            {
                body.ReportDiagnostic(TinyhandBody.Error_UnionNotImplementing, item.UnionTo.Location, subObject.FullName, item.BaseObject.FullName);
                return;
            }
            else
            {
                body.ReportDiagnostic(TinyhandBody.Error_UnionNotDerived, item.UnionTo.Location, subObject.FullName, item.BaseObject.FullName);
                return;
            }
        }

        List<TinyhandUnionAttributeMock> list;
        if (item.BaseObject.Union == null)
        {
            list = new();
            item.BaseObject.Union = new(item.BaseObject, list);
        }
        else
        {
            list = item.BaseObject.Union.UnionList;
        }

        subObject.GetRawInformation(out var symbol, out _, out _);
        if (symbol == null)
        {
            return;
        }

        foreach (var x in list)
        {
            if (x.Key == item.UnionTo.Key)
            {
                body.AddDiagnostic(TinyhandBody.Error_IntKeyConflicted, item.UnionTo.Location);
                return;
            }
            else if (x.SubType == symbol)
            {
                body.AddDiagnostic(TinyhandBody.Error_SubtypeConflicted, item.UnionTo.Location);
                return;
            }
        }

        var atr = new TinyhandUnionAttributeMock(item.UnionTo.Key, symbol, item.UnionTo.Location);
        list.Add(atr);
    }*/

    public TinyhandUnion(TinyhandObject obj, List<TinyhandUnionAttributeMock> unionList)
    {
        this.Object = obj;
        this.UnionList = unionList;
    }

    public TinyhandObject Object { get; }

    public List<TinyhandUnionAttributeMock> UnionList { get; }

    public SortedDictionary<int, TinyhandObject>? UnionDictionary { get; private set; }

    public void CheckAndPrepare()
    {
        // Create SortedDictionary
        var unionDictionary = new SortedDictionary<int, TinyhandObject>();
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
                {
                    unionDictionary.Add(x.Key, obj);
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

        this.UnionDictionary = unionDictionary;
    }

    internal void GenerateFormatter_Serialize(ScopingStringBuilder ssb, GeneratorInformation info)
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
    }

    internal void GenerateFormatter_Serialize2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.UnionDictionary == null)
        {
            return;
        }

        ssb.AppendLine($"if ({ssb.FullObject} == null) {{ writer.WriteNil(); return; }}");
        ssb.AppendLine("writer.WriteArrayHeader(2);");
        ssb.AppendLine($"var type = {ssb.FullObject}.GetType();");

        var firstFlag = true;
        foreach (var x in this.UnionDictionary)
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
                ssb.AppendLine("writer.Write(" + x.Key.ToString() + ");");
                ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, Unsafe.As<{x.Value.FullName}>({ssb.FullObject}), options);");
            }
        }

        using (ssb.ScopeBrace("else"))
        {
            ssb.AppendLine("writer.WriteNil();");
            ssb.AppendLine("writer.WriteNil();");
        }
    }

    internal void GenerateFormatter_Deserialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.UnionDictionary == null)
        {
            return;
        }

        ssb.AppendLine($"if (reader.TryReadNil()) {{ return; }}");
        ssb.AppendLine("if (reader.ReadArrayHeader() != 2) { throw new TinyhandException(\"Invalid Union data was detected.\"); }");
        ssb.AppendLine($"if (reader.TryReadNil()) {{ reader.ReadNil(); return; }}");

        ssb.AppendLine("var key = reader.ReadInt32();");
        using (var sw = ssb.ScopeBrace("switch (key)"))
        {
            foreach (var x in this.UnionDictionary)
            {
                var keyString = x.Key.ToString();
                var name = "x" + keyString;
                ssb.AppendLine("case " + keyString + ":");
                ssb.IncrementIndent();

                ssb.AppendLine($"var {name} = Unsafe.As<{x.Value.FullName + x.Value.QuestionMarkIfReferenceType}>({ssb.FullObject});");
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
