// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Arc.Visceral;

public enum VisceralGenericsKind
{
    NotSet = 0,
    NotGeneric = 1,
    // UnboundGeneric = 2, // Currently not supported.
    OpenGeneric = 3,
    ClosedGeneric = 4,
}

/// <summary>
/// Process generic syntax.
/// </summary>
public class VisceralGenerics
{
    public VisceralGenerics()
    {
    }

    public void Add(GenericNameSyntax genericSyntax)
    {
        GenericsItem item;
        var identification = new GenericsIdentification(genericSyntax);

        if (!this.ItemDictionary.TryGetValue(identification, out item))
        {
            item = new GenericsItem(identification, genericSyntax);
            this.ItemDictionary.Add(identification, item);
        }
    }

    /// <summary>
    /// Resolves the generic names.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="isCandidate">Selects the type names to resolve; the other generic names are left <see cref="VisceralGenericsKind.NotSet"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public void Prepare(Compilation compilation, Func<string, bool>? isCandidate = null, CancellationToken cancellationToken = default)
    {
        // Binding a generic name binds its whole member body, so the names are bound with one semantic model per syntax tree
        // (a model keeps the bound bodies), and the trees are bound concurrently unless the compilation disables concurrent builds.
        var trees = this.ItemDictionary.Values
            .Where(x => isCandidate is null || isCandidate(x.GenericSyntax.Identifier.ValueText))
            .GroupBy(x => x.GenericSyntax.SyntaxTree).ToArray();
        void Bind(IGrouping<SyntaxTree, GenericsItem> items)
        {
            var model = compilation.GetSemanticModel(items.Key);
            foreach (var x in items)
            {
                if (model.GetSymbolInfo(x.GenericSyntax, cancellationToken).Symbol is INamedTypeSymbol ts)
                {
                    x.TypeSymbol = ts;
                    x.GenericsKind = VisceralHelper.TypeToGenericsKind(ts);
                }
            }
        }

        if (compilation.Options.ConcurrentBuild)
        {
            Parallel.ForEach(trees, new ParallelOptions { CancellationToken = cancellationToken }, Bind);
        }
        else
        {
            foreach (var x in trees)
            {
                Bind(x);
            }
        }
    }

    internal Dictionary<GenericsIdentification, GenericsItem> ItemDictionary { get; } = new();

    internal class GenericsItem
    {
        public GenericsIdentification Identification { get; }

        public GenericNameSyntax GenericSyntax { get; }

        public INamedTypeSymbol? TypeSymbol { get; set; }

        public VisceralGenericsKind GenericsKind { get; set; }

        public GenericsItem(GenericsIdentification identification, GenericNameSyntax genericSyntax)
        {
            this.Identification = identification;
            this.GenericSyntax = genericSyntax;
        }

        public override string ToString() => this.Identification.ToString();
    }

    internal readonly struct GenericsIdentification
    {// Generics Identification
        public GenericsIdentification(GenericNameSyntax genericSyntax)
        {
            this.TypeName = genericSyntax.Identifier.ValueText;

            var length = genericSyntax.TypeArgumentList.Arguments.Count;
            this.Arguments = new string[length];
            for (var i = 0; i < length; i++)
            {
                this.Arguments[i] = genericSyntax.TypeArgumentList.Arguments[i].ToString();
            }
        }

        private readonly string TypeName;
        private readonly string[] Arguments;

        public override int GetHashCode()
        {// Consider HashCode.Combine();
            unchecked
            {
                var hash = (17 * 31) + this.TypeName.GetHashCode();
                foreach (var x in this.Arguments)
                {
                    hash = (hash * 31) + x.GetHashCode();
                }

                return hash;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(GenericsIdentification))
            {
                return false;
            }

            var target = (GenericsIdentification)obj;
            if (this.TypeName != target.TypeName)
            {
                return false;
            }
            else if (this.Arguments.Length != target.Arguments.Length)
            {
                return false;
            }

            for (var i = 0; i < this.Arguments.Length; i++)
            {
                if (this.Arguments[i] != target.Arguments[i])
                {
                    return false;
                }
            }

            // Identical
            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(this.TypeName);
            sb.Append('<');
            for (var i = 0; i < this.Arguments.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(this.Arguments[i]);
            }

            sb.Append('>');
            return sb.ToString();
        }
    }
}
