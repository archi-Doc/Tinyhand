// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Tinyhand.Coders;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Generator
{
    public enum ReconstructCondition
    {
        None,
        Can,
        CircularDependency,
        NoDefaultConstructor,
        NotReferenceType,
    }

    internal static class TinyhandReconstruct
    {
        internal static ReconstructCondition GetReconstructCondition(TinyhandObject obj)
        {
            var typeObject = obj.TypeObject;
            if (typeObject == null)
            {
                return ReconstructCondition.NotReferenceType;
            }

            if (typeObject.Kind == VisceralObjectKind.Interface && typeObject.ObjectAttribute != null)
            {
                return ReconstructCondition.NoDefaultConstructor;
            }

            if (typeObject.ObjectAttribute != null || typeObject.Kind.IsType())
            {// TinyhandObject or Reference/Value type, check circular dependency.
                if (CheckCircular(typeObject))
                {
                    return ReconstructCondition.CircularDependency;
                }

                if (typeObject.ObjectAttribute != null)
                {// TinyhandObject
                    return ReconstructCondition.Can;
                }
            }

            if (obj.TypeObjectWithNullable != null && CoderResolver.Instance.TryGetCoder(obj.TypeObjectWithNullable) != null)
            {// Coder found
                return ReconstructCondition.Can;
            }

            if (typeObject.Kind.IsReferenceType())
            {// Reference type
                var hasDefaultConstructor = typeObject.TypeObject?.GetMembers(VisceralTarget.Method).Any(a => a.Method_IsConstructor && a.Method_Parameters.Length == 0) == true;
                if (!hasDefaultConstructor)
                {
                    return ReconstructCondition.NoDefaultConstructor;
                }

                return ReconstructCondition.Can;
            }
            else if (typeObject.Kind.IsValueType())
            {// Value type
                return ReconstructCondition.Can;
            }

            return ReconstructCondition.NotReferenceType;
        }

        /*internal static bool IsBuiltinReconstructable(string simpleName) => simpleName switch
        {
            "string" => true,
            "string" => true,
            "string" => true,
            "string" => true,
            "string" => true,
            "string" => true,
            "string" => true,
            "string" => true,
            "string" => true,
            "string" => true,
        }*/

        /// <summary>
        /// Check circular dependency.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>Returns true if circular dependency detected.</returns>
        internal static bool CheckCircular(TinyhandObject obj)
        {
            var ret = false;
            var circularCheck = new Stack<TinyhandObject>();

            CheckCircularCore(obj);

            return ret;

            void CheckCircularCore(TinyhandObject obj)
            {
                var typeObject = obj.TypeObject;
                if (typeObject == null)
                {
                    return;
                }

                circularCheck.Push(typeObject);

                try
                {
                    foreach (var x in typeObject.Members)
                    {
                        if (ret)
                        {
                            return;
                        }
                        else if (x.TypeObject == null)
                        {
                            continue;
                        }
                        else if (circularCheck.Contains(x.TypeObject))
                        {// Circular dependency
                            ret = true;
                            return;
                        }
                        else
                        {
                            CheckCircularCore(x);
                        }
                    }
                }
                finally
                {
                    circularCheck.Pop();
                }
            }
        }
    }
}
