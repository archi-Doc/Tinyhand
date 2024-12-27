// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Visceral;
using Tinyhand.Generator;

namespace TinyhandGenerator.Internal;

internal ref struct ValueAssignment
{
    private ScopingStringBuilder? ssb;
    private string destObject = string.Empty;
    private GeneratorInformation info;
    private TinyhandObject? parent;
    private TinyhandObject? @object;
    private bool emptyBrace;

    private ScopingStringBuilder.IScope? initSetter;
    private ScopingStringBuilder.IScope? braceScope;

    public ValueAssignment(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject parent, TinyhandObject @object, bool emptyBrace = false)
    {
        this.ssb = ssb;
        this.destObject = this.ssb.FullObject;
        this.info = info;
        this.parent = parent;
        this.@object = @object;
        this.emptyBrace = emptyBrace;
    }

    public void Start()
    {
        var withNullable = this.@object?.TypeObjectWithNullable;
        if (this.ssb is null || this.info is null || this.parent is null || this.@object is null || withNullable is null)
        {
            return;
        }

        if (this.@object.RefFieldDelegate is not null || this.@object.SetterDelegate is not null || this.@object.IsReadOnly)
        {// TypeName vd;
            if (this.emptyBrace)
            {
                this.ssb.ScopeBrace(string.Empty);
            }

            this.initSetter = this.ssb.ScopeFullObject("vd");
            this.ssb.AppendLine(withNullable.FullNameWithNullable + " vd;");
        }
    }

    public void End()
    {
        var withNullable = this.@object?.TypeObjectWithNullable;
        if (this.ssb is null || this.info is null || this.parent is null || this.@object is null || withNullable is null)
        {
            return;
        }

        if (this.initSetter != null)
        {
            this.initSetter.Dispose();
            this.initSetter = null;

            if (this.@object.RefFieldDelegate is not null)
            {// RefFieldDelegate(obj) = vd;
                var prefix = this.info.GeneratingStaticMethod ? (this.parent.RegionalName + ".") : string.Empty;
                this.ssb.AppendLine($"{prefix}{this.@object.RefFieldDelegate}({this.parent.InIfStruct}{this.destObject}) = vd;");
            }
            else if (this.@object.SetterDelegate is not null)
            {// SetterDelegate!(obj, vd);
                var prefix = this.info.GeneratingStaticMethod ? (this.parent.RegionalName + ".") : string.Empty;
                this.ssb.AppendLine($"{prefix}{this.@object.SetterDelegate}!({this.parent.InIfStruct}{this.destObject}, vd);");
            }
            else if (this.@object.IsReadOnly)
            {
                if (withNullable.Object.IsUnmanagedType)
                {// fixed (ulong* ptr = &this.Id0) *ptr = 11;
                    this.ssb.AppendLine($"fixed ({withNullable.FullNameWithNullable}* ptr = &{this.destObject}.{this.@object.SimpleName}) *ptr = vd;");
                }
                else
                {// Unsafe.AsRef(in {this.array) = vd;
                    this.ssb.AppendLine($"Unsafe.AsRef(in {this.destObject}.{this.@object.SimpleName}) = vd;");
                }
            }
        }
    }
}
