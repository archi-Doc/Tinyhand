// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Tinyhand.Generator
{
    public class TinyhandUnion
    {
        public static TinyhandUnion? CreateFromObject(TinyhandObject obj)
        {
            List<TinyhandUnionAttributeMock>? unionList = null;
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
                        continue;
                    }

                    if (attr.SubType == null)
                    {
                        obj.Body.ReportDiagnostic(TinyhandBody.Error_NullSubtype, x.Location);
                        continue;
                    }

                    if (unionList == null)
                    {
                        unionList = new();
                    }

                    unionList.Add(attr);
                }
            }

            if (obj.Body.Abort)
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
                }

                if (!checker2.Add(item.SubType))
                {
                    obj.Body.ReportDiagnostic(TinyhandBody.Error_SubtypeConflicted, item.Location);
                }
            }

            if (obj.Body.Abort)
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

        public void CheckAndPrepare()
        {
            // Create SortedDictionary
            var unionDictionary = new SortedDictionary<int, TinyhandObject>();
            foreach (var x in this.UnionList)
            {
                if (this.Object.Body.TryGet(x.SubType!, out var obj) && obj.ObjectAttribute != null)
                {
                    unionDictionary.Add(x.Key, obj);
                }
                else
                {
                    this.Object.Body.ReportDiagnostic(TinyhandBody.Error_UnionTargetError, x.Location);
                }
            }
        }
    }
}
