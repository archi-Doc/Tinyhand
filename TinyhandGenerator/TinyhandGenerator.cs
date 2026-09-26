// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS1036

namespace Tinyhand.Generator;

/// <summary>
/// Generates the serializers of Tinyhand objects.<br/>
/// Roslyn shares a generator instance among runs, which may execute concurrently (e.g. for projects or target frameworks
/// that use the same analyzer), so the generator itself has no state; each run keeps its own in <see cref="TinyhandGeneratorRun"/>.
/// </summary>
[Generator]
public class TinyhandGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.CompilationProvider.Combine(
            context.SyntaxProvider
            .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Collect());

        context.RegisterImplementationSourceOutput(provider, Emit);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0)
        {
            return true;
        }
        else if (node is GenericNameSyntax { })
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static CSharpSyntaxNode? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        if (context.Node is TypeDeclarationSyntax typeSyntax)
        {
            foreach (var attributeList in typeSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (name.EndsWith(TinyhandGeneratorOptionAttributeData.Name) ||
                        name.EndsWith(TinyhandGeneratorOptionAttributeData.SimpleName))
                    {
                        return typeSyntax;
                    }
                    else if (name.EndsWith(TinyhandGenerateMemberAttributeData.Name) ||
                        name.EndsWith(TinyhandGenerateMemberAttributeData.SimpleName) ||
                        name.EndsWith(TinyhandGenerateHashAttributeData.Name) ||
                        name.EndsWith(TinyhandGenerateHashAttributeData.SimpleName))
                    {
                        return typeSyntax;
                    }
                    else if (name.EndsWith(TinyhandObjectAttributeData.Name) ||
                        name.EndsWith(TinyhandObjectAttributeData.SimpleName) ||
                        name.EndsWith(TinyhandUnionAttributeData.Name) ||
                        name.EndsWith(TinyhandUnionAttributeData.SimpleName))
                    {
                        return typeSyntax;
                    }
                }
            }
        }
        else if (context.Node is GenericNameSyntax genericSyntax)
        {
            return genericSyntax;
        }

        return null;
    }

    private static void Emit(SourceProductionContext context, (Compilation Compilation, ImmutableArray<CSharpSyntaxNode?> Types) source)
    {// The generated code must not depend on the culture of the compiler process (e.g. U+2212 as the negative sign of an interpolated number).
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        try
        {
            new TinyhandGeneratorRun().Emit(context, source.Compilation, source.Types);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }
}
