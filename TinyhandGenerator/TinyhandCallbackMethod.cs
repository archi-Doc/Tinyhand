// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Visceral;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Tinyhand.Generator;

[Flags]
public enum CallbackKind
{
    None,
    OnSerializing,
    OnSerialized,
    OnDeserializing,
    OnDeserialized,
    // AbandonReconstructCode OnReconstructing,
    // AbandonReconstructCode OnReconstructed,
}

public class CallbackMethod
{
    public static CallbackMethod? TryCreate(TinyhandObject method)
    {
        var kind = CallbackKind.None;
        var error = false;
        foreach (var y in method.AllAttributes)
        {
            var currentKind = CallbackKind.None;
            if (y.FullName == TinyhandOnSerializingAttributeMock.FullName)
            {// OnSerializing
                if (!method.IsStatic && method.Method_Parameters.Length == 0)
                {
                    currentKind = CallbackKind.OnSerializing;
                }
                else
                {
                    method.Body.ReportDiagnostic(TinyhandBody.Error_CallbackMetohd, method.Location);
                    error = true;
                }
            }
            else if (y.FullName == TinyhandOnSerializedAttributeMock.FullName)
            {// OnSerialized
                if (!method.IsStatic && method.Method_Parameters.Length == 0)
                {
                    currentKind = CallbackKind.OnSerialized;
                }
                else
                {
                    method.Body.ReportDiagnostic(TinyhandBody.Error_CallbackMetohd, method.Location);
                    error = true;
                }
            }
            else if (y.FullName == TinyhandOnDeserializingAttributeMock.FullName)
            {// OnDeserializing
                if (!method.IsStatic && method.Method_Parameters.Length == 0)
                {
                    currentKind = CallbackKind.OnDeserializing;
                }
                else
                {
                    method.Body.ReportDiagnostic(TinyhandBody.Error_CallbackMetohd, method.Location);
                    error = true;
                }
            }
            else if (y.FullName == TinyhandOnDeserializedAttributeMock.FullName)
            {// OnDeserialized
                if (!method.IsStatic && method.Method_Parameters.Length == 0)
                {
                    currentKind = CallbackKind.OnDeserialized;
                }
                else
                {
                    method.Body.ReportDiagnostic(TinyhandBody.Error_CallbackMetohd, method.Location);
                    error = true;
                }
            }

            // AbandonReconstructCode
            /* else if (y.FullName == TinyhandOnReconstructingAttributeMock.FullName)
            {// OnReconstructing
                if (!method.IsStatic && method.Method_Parameters.Length == 0)
                {
                    currentKind = CallbackKind.OnReconstructing;
                }
                else
                {
                    method.Body.ReportDiagnostic(TinyhandBody.Error_CallbackMetohd, method.Location);
                    error = true;
                }
            }
            else if (y.FullName == TinyhandOnReconstructedAttributeMock.FullName)
            {// OnReconstructed
                if (!method.IsStatic && method.Method_Parameters.Length == 0)
                {
                    currentKind = CallbackKind.OnReconstructed;
                }
                else
                {
                    method.Body.ReportDiagnostic(TinyhandBody.Error_CallbackMetohd, method.Location);
                    error = true;
                }
            }*/

            if (currentKind != CallbackKind.None)
            {
                if (kind == CallbackKind.None)
                {
                    kind = currentKind;
                }
                else
                {
                    method.Body.ReportDiagnostic(TinyhandBody.Error_CallbackAttribute, y.Location);
                    error = true;
                }
            }
        }

        if (error || kind == CallbackKind.None)
        {
            return default;
        }

        return new(kind, method);
    }

    private CallbackMethod(CallbackKind kind, TinyhandObject method)
    {
        this.Kind = kind;
        this.Method = method;
    }

    public CallbackKind Kind { get; }

    public TinyhandObject Method { get; }

    public void Generate(ScopingStringBuilder ssb)
    {
        if (this.Kind != CallbackKind.None)
        {
            ssb.AppendLine($"{ssb.FullObject}.{this.Method.SimpleName}();");
        }

        /*if (this.Kind == CallbackKind.OnSerializing)
        {
            this.Generate_OnSerializing(ssb);
        }*/
        }

        /*public void Generate_OnSerializing(ScopingStringBuilder ssb)
        {
        }

        public void Generate_OnSerialized(ScopingStringBuilder ssb)
        {
        }*/
    }
