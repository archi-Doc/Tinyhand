// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS1036 // Follow the analyzer configuration of the existing generator.
#pragma warning disable RS2008 // Diagnostics are shipped with Tinyhand's generator.

namespace Tinyhand.Generator;

/// <summary>Emits registrations with concrete type arguments, including formatter dependencies.</summary>
[Generator]
public sealed class StaticRegistrationGenerator : IIncrementalGenerator
{
    private const string Resolver = "global::Tinyhand.Resolvers.GeneratedResolver";
    private static readonly DiagnosticDescriptor InaccessibleType = new(
        "THAOT001", "Cannot generate static registration", "The containing types of '{0}' must be partial to generate its NativeAOT registration", "Tinyhand", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor UnboundedType = new(
        "THAOT002", "Static registration graph is too large", "Type '{0}' exceeds the static registration limit (64 levels or 4096 type nodes); check for recursively expanding generic types or helpers", "Tinyhand", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor OpenRoot = new(
        "THAOT003", "Registration requires a closed type", "TinyhandRegister requires a closed type, but '{0}' contains unspecified type arguments", "Tinyhand", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var usages = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is TypeSyntax or InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or TupleExpressionSyntax or TypeDeclarationSyntax,
            static (ctx, token) =>
            {
                if (ctx.Node is TypeDeclarationSyntax declaration)
                {
                    return new Usage(ctx.SemanticModel.GetDeclaredSymbol(declaration, token) as ITypeSymbol, null);
                }

                var type = ctx.SemanticModel.GetTypeInfo(ctx.Node, token).Type;
                var method = ctx.Node is InvocationExpressionSyntax invocation
                    ? ctx.SemanticModel.GetSymbolInfo(invocation, token).Symbol as IMethodSymbol
                    : null;
                return new Usage(type, method);
            }).Collect();
        context.RegisterSourceOutput(context.CompilationProvider.Combine(usages), static (ctx, source) =>
        {
            try
            {
                new Emitter(source.Left, ctx).Emit(source.Right);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("THAOT999", "Static registration failed", "{0}", "Tinyhand", DiagnosticSeverity.Error, true), null, exception.ToString().Replace('\r', ' ').Replace('\n', ' ')));
            }
        });
    }

    private readonly struct Usage
    {
        internal Usage(ITypeSymbol? type, IMethodSymbol? method)
        {
            this.Type = type;
            this.Method = method;
        }

        internal ITypeSymbol? Type { get; }

        internal IMethodSymbol? Method { get; }
    }

    private sealed class Emitter
    {
        private readonly Compilation compilation;
        private readonly SourceProductionContext context;
        private readonly HashSet<ITypeSymbol> types = new(SymbolEqualityComparer.Default);
        private readonly HashSet<ITypeSymbol> requiredTypes = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<ITypeSymbol, HashSet<ITypeSymbol>> dependencies = new(SymbolEqualityComparer.Default);
        private readonly Queue<ITypeSymbol> pending = new();
        private readonly HashSet<IMethodSymbol> methods = new(SymbolEqualityComparer.Default);
        private readonly Queue<IMethodSymbol> pendingMethods = new();
        private readonly List<(ITypeSymbol Type, string Code)> registrations = new();
        private bool reportedDepth;
        private ITypeSymbol? currentType;

        internal Emitter(Compilation compilation, SourceProductionContext context)
        {
            this.compilation = compilation;
            this.context = context;
        }

        internal void Emit(ImmutableArray<Usage> usages)
        {
            if (this.compilation.GetTypeByMetadataName("Tinyhand.Resolvers.GeneratedResolver") is null)
            {
                return;
            }

            foreach (var usage in usages)
            {
                this.Add(usage.Type);
                this.AddMethod(usage.Method);
            }

            // Explicit roots also cover types referenced only from other assemblies.
            foreach (var attribute in this.compilation.Assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == "Tinyhand.TinyhandRegisterAttribute")
                {
                    foreach (var argument in attribute.ConstructorArguments)
                    {
                        if (argument.Value is ITypeSymbol root)
                        {
                            if (!IsClosed(root))
                            {
                                this.context.ReportDiagnostic(Diagnostic.Create(OpenRoot, attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(), Name(root)));
                            }
                            else
                            {
                                this.requiredTypes.Add(root);
                                this.Add(root);
                            }
                        }
                    }
                }
            }

            while (this.pending.Count > 0 || this.pendingMethods.Count > 0)
            {
                this.context.CancellationToken.ThrowIfCancellationRequested();
                if (this.pendingMethods.Count > 0)
                {
                    this.ProcessMethod(this.pendingMethods.Dequeue());
                }
                else
                {
                    this.currentType = this.pending.Dequeue();
                    this.Process(this.currentType);
                    this.currentType = null;
                }
            }

            var required = new Queue<ITypeSymbol>(this.requiredTypes);
            while (required.Count > 0)
            {
                if (this.dependencies.TryGetValue(required.Dequeue(), out var children))
                {
                    foreach (var child in children)
                    {
                        if (this.requiredTypes.Add(child))
                        {
                            required.Enqueue(child);
                        }
                    }
                }
            }

            var initializer = new StringBuilder();
            var bridges = new StringBuilder();
            var index = 0;
            foreach (var registration in this.registrations.OrderBy(x => Name(x.Type), StringComparer.Ordinal))
            {
                var code = registration.Code;
                if (!this.compilation.IsSymbolAccessibleWithin(registration.Type, this.compilation.Assembly))
                {
                    var scope = this.FindScope(registration.Type);
                    if (scope is null)
                    {
                        if (this.requiredTypes.Contains(registration.Type))
                        {
                            this.context.ReportDiagnostic(Diagnostic.Create(InaccessibleType, registration.Type.Locations.FirstOrDefault(), Name(registration.Type)));
                        }

                        continue;
                    }

                    var methodName = "__TinyhandRegister" + index++;
                    code = this.Bridge(scope, code, methodName, bridges);
                }

                initializer.AppendLine(code);
            }

            var assembly = new string((this.compilation.AssemblyName ?? "Assembly").Select(x => char.IsLetterOrDigit(x) ? x : '_').ToArray());
            var source = "// <auto-generated/>\n#nullable enable\n#pragma warning disable CS1591, CS8714, CS8631, CS0618\n" +
                "internal static class TinyhandStaticRegistration_" + assembly + "\n{\nprivate static int initialized;\n[global::System.Runtime.CompilerServices.ModuleInitializer]\ninternal static void Initialize()\n{\nif (global::System.Threading.Interlocked.Exchange(ref initialized, 1) != 0) return;\n" + initializer + "}\n}\n" + bridges;
            this.context.AddSource("Tinyhand.StaticRegistration.g.cs", source);
        }

        private static string Name(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        private static bool IsClosed(ITypeSymbol type) => type switch
        {
            ITypeParameterSymbol => false,
            IArrayTypeSymbol array => IsClosed(array.ElementType),
            INamedTypeSymbol named => named.TypeKind != TypeKind.Error && !named.IsAnonymousType && !named.IsUnboundGenericType && (named.ContainingType is null || IsClosed(named.ContainingType)) && named.TypeArguments.All(IsClosed),
            _ => type.TypeKind is not (TypeKind.Error or TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.Dynamic),
        };

        private static string MetadataName(INamedTypeSymbol type) => type.OriginalDefinition.ContainingNamespace.ToDisplayString() + "." + type.OriginalDefinition.MetadataName;

        private bool CheckDepth(ITypeSymbol type)
        {
            // A node budget also bounds repeated branches such as Grow<Pair<T,T>>.
            // A depth-only check can take exponential time on these type graphs.
            var nodesRemaining = 4096;
            bool WithinLimit(ITypeSymbol candidate, int remaining) => remaining > 0 && --nodesRemaining >= 0 && candidate switch
            {
                IArrayTypeSymbol array => WithinLimit(array.ElementType, remaining - 1),
                INamedTypeSymbol named => (named.ContainingType is null || WithinLimit(named.ContainingType, remaining - 1)) && named.TypeArguments.All(x => WithinLimit(x, remaining - 1)),
                _ => true,
            };

            if (WithinLimit(type, 64))
            {
                return true;
            }

            if (!this.reportedDepth)
            {
                this.reportedDepth = true;
                this.context.ReportDiagnostic(Diagnostic.Create(UnboundedType, type.Locations.FirstOrDefault(), type.Name));
            }

            return false;
        }

        private void Add(ITypeSymbol? type)
        {
            if (type is null || type.SpecialType == SpecialType.System_Void || type.IsRefLikeType || !this.CheckDepth(type) || !IsClosed(type))
            {
                return;
            }

            if (type is INamedTypeSymbol { IsTupleType: true, TupleUnderlyingType: { } underlying })
            {
                type = underlying;
            }

            type = type.WithNullableAnnotation(NullableAnnotation.None);
            if (this.currentType is { } parent)
            {
                if (!this.dependencies.TryGetValue(parent, out var children))
                {
                    children = new(SymbolEqualityComparer.Default);
                    this.dependencies.Add(parent, children);
                }

                children.Add(type);
            }

            if (this.types.Add(type))
            {
                this.pending.Enqueue(type);
            }
        }

        private void AddMethod(IMethodSymbol? method)
        {
            if (method is null || !this.CheckDepth(method.ContainingType) || !method.TypeArguments.All(this.CheckDepth) ||
                !IsClosed(method.ContainingType) || !method.TypeArguments.All(IsClosed) || !this.methods.Add(method))
            {
                return;
            }

            this.pendingMethods.Enqueue(method);
        }

        private void ProcessMethod(IMethodSymbol method)
        {
            foreach (var argument in method.TypeArguments)
            {
                this.Add(argument);
                if (method.ContainingType.ToDisplayString() is "Tinyhand.TinyhandSerializer" or "Tinyhand.TinyhandTypeIdentifier")
                {
                    this.requiredTypes.Add(argument);
                }
            }

            // Follow substitutions inside generic helpers (e.g. Serialize<T[]> in a
            // helper invoked as Helper<MyStruct>), without constructing runtime types.
            if ((!method.IsGenericMethod && !method.ContainingType.IsGenericType) || method.DeclaringSyntaxReferences.Length == 0)
            {
                return;
            }

            var substitutions = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            for (var containing = method.ContainingType; containing is not null; containing = containing.ContainingType)
            {
                for (var i = 0; i < containing.TypeArguments.Length; i++)
                {
                    substitutions[containing.OriginalDefinition.TypeParameters[i]] = containing.TypeArguments[i];
                }
            }

            for (var i = 0; i < method.TypeArguments.Length; i++)
            {
                substitutions[method.OriginalDefinition.TypeParameters[i]] = method.TypeArguments[i];
            }

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var syntax = reference.GetSyntax(this.context.CancellationToken);
                var model = this.compilation.GetSemanticModel(syntax.SyntaxTree);
                foreach (var node in syntax.DescendantNodes().OfType<TypeSyntax>())
                {
                    this.Add(this.Substitute(model.GetTypeInfo(node, this.context.CancellationToken).Type, substitutions));
                }

                foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (model.GetSymbolInfo(invocation, this.context.CancellationToken).Symbol is not IMethodSymbol invoked)
                    {
                        continue;
                    }

                    var containing = (INamedTypeSymbol)this.Substitute(invoked.ContainingType, substitutions)!;
                    var definition = containing.GetMembers(invoked.Name).OfType<IMethodSymbol>()
                        .FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, invoked.OriginalDefinition)) ?? invoked.ConstructedFrom;
                    this.AddMethod(invoked.IsGenericMethod
                        ? definition.Construct(invoked.TypeArguments.Select(x => this.Substitute(x, substitutions)!).ToArray())
                        : definition);
                }
            }
        }

        private ITypeSymbol? Substitute(ITypeSymbol? type, Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
        {
            if (type is ITypeParameterSymbol parameter)
            {
                return substitutions.TryGetValue(parameter, out var argument) ? argument : type;
            }

            if (type is IArrayTypeSymbol array)
            {
                return this.compilation.CreateArrayTypeSymbol(this.Substitute(array.ElementType, substitutions)!, array.Rank);
            }

            if (type is INamedTypeSymbol named && named.TypeKind != TypeKind.Error && named.IsGenericType && !named.IsUnboundGenericType)
            {
                var definition = named.OriginalDefinition;
                if (named.ContainingType is { } containing)
                {
                    var parent = (INamedTypeSymbol)this.Substitute(containing, substitutions)!;
                    definition = parent.GetTypeMembers(named.Name, named.Arity).First();
                }

                return named.Arity == 0 ? definition : definition.Construct(named.TypeArguments.Select(x => this.Substitute(x, substitutions)!).ToArray());
            }

            return type;
        }

        private void AddAttributeTypes(TypedConstant argument)
        {
            if (argument.Value is ITypeSymbol type)
            {
                this.Add(type);
            }
        }

        private void Process(ITypeSymbol type)
        {
            var name = Name(type);
            if (type is IArrayTypeSymbol array)
            {
                this.Add(array.ElementType);
                if (array.Rank <= 4)
                {
                    this.registrations.Add((type, $"{Resolver}.RegisterArray{(array.Rank == 1 ? string.Empty : array.Rank.ToString())}<{Name(array.ElementType)}>();"));
                }

                return;
            }

            if (type is not INamedTypeSymbol named)
            {
                return;
            }

            foreach (var argument in named.TypeArguments)
            {
                this.Add(argument);
            }

            if (named.ContainingType is { } parent)
            {
                this.Add(parent);
            }

            var metadata = MetadataName(named);
            if (named.TypeKind == TypeKind.Enum)
            {
                this.registrations.Add((type, $"{Resolver}.RegisterEnum<{name}>();"));
                this.Add(this.compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(named));
            }
            else if (FormatterCatalog.Registrations.TryGetValue(metadata, out var registration))
            {
                this.registrations.Add((type, $"{Resolver}.{registration}<{string.Join(", ", named.TypeArguments.Select(Name))}>();"));
                if (named.IsValueType && metadata != "System.Nullable`1")
                {
                    this.Add(this.compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(named));
                }

                if (metadata is "System.Memory`1" or "System.ReadOnlyMemory`1" or "System.Buffers.ReadOnlySequence`1" or "System.ArraySegment`1")
                {
                    this.Add(this.compilation.CreateArrayTypeSymbol(named.TypeArguments[0]));
                }

                if (metadata == "System.ArraySegment`1")
                {
                    this.Add(this.compilation.GetTypeByMetadataName("System.Memory`1")!.Construct(named.TypeArguments[0]));
                }

                if (metadata == "System.Memory`1")
                {
                    this.Add(this.compilation.GetTypeByMetadataName("System.ReadOnlyMemory`1")!.Construct(named.TypeArguments[0]));
                }

                if (metadata == "System.Linq.ILookup`2")
                {
                    this.Add(this.compilation.GetTypeByMetadataName("System.Linq.IGrouping`2")!.Construct(named.TypeArguments.ToArray()));
                }

                if (metadata == "System.Linq.IGrouping`2")
                {
                    this.Add(this.compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")!.Construct(named.TypeArguments[1]));
                }
            }
            else if (named.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() is "Tinyhand.TinyhandObjectAttribute" or "Tinyhand.TinyhandUnionAttribute") ||
                named.AllInterfaces.Any(x => MetadataName(x) == "Tinyhand.ITinyhandSerializable`1"))
            {
                if ((named.IsAbstract || named.TypeKind == TypeKind.Interface) && !named.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "Tinyhand.TinyhandUnionAttribute"))
                {
                    return;
                }

                var code = $"{Resolver}.RegisterObject<{name}>();";
                if (named.AllInterfaces.Any(x => MetadataName(x) == "Arc.IStringConvertible`1"))
                {
                    code += $"\nglobal::Tinyhand.TinyhandTypeIdentifier.RegisterStringConvertible<{name}>();";
                }

                foreach (var attribute in named.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() == "Tinyhand.TinyhandObjectAttribute" && attribute.NamedArguments.Any(x => x.Key == "AddImmutable" && x.Value.Value is true))
                    {
                        code += $"\n{Resolver}.RegisterObject<{name}.Immutable>();";
                    }

                    foreach (var argument in attribute.ConstructorArguments)
                    {
                        this.AddAttributeTypes(argument);
                    }
                }

                this.registrations.Add((type, code));
                if (named.IsValueType)
                {
                    this.Add(this.compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(named));
                }

                for (var current = named; current is not null; current = current.BaseType)
                {
                    foreach (var member in current.GetMembers())
                    {
                        if (member.IsStatic || member.IsImplicitlyDeclared || member.GetAttributes().Any(x => x.AttributeClass?.Name == "IgnoreMemberAttribute"))
                        {
                            continue;
                        }

                        this.Add(member switch { IFieldSymbol f => f.Type, IPropertySymbol p => p.Type, _ => null });
                    }
                }
            }
            else if (metadata != "System.Dynamic.ExpandoObject" && !named.IsAbstract && named.TypeKind == TypeKind.Class && named.InstanceConstructors.Any(x => x.Parameters.Length == 0 && x.DeclaredAccessibility == Accessibility.Public))
            {
                var dictionary = named.AllInterfaces.FirstOrDefault(x => MetadataName(x) == "System.Collections.Generic.IDictionary`2");
                if (dictionary is not null)
                {
                    var key = dictionary.TypeArguments[0];
                    var value = dictionary.TypeArguments[1];
                    this.Add(key);
                    this.Add(value);
                    var comparer = this.compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1")!.Construct(key);
                    var capacityConstructor = named.InstanceConstructors.Any(x => x.DeclaredAccessibility == Accessibility.Public && x.Parameters.Length == 2 &&
                        x.Parameters[0].Type.SpecialType == SpecialType.System_Int32 && SymbolEqualityComparer.Default.Equals(x.Parameters[1].Type, comparer));
                    var constructor = capacityConstructor ? $"new {name}(count, comparer)" : $"new {name}()";
                    this.registrations.Add((type, $"{Resolver}.RegisterDictionary<{Name(key)}, {Name(value)}, {name}>(static (count, comparer) => {constructor});"));
                }
                else if (named.AllInterfaces.FirstOrDefault(x => MetadataName(x) == "System.Collections.Generic.ICollection`1") is { } collection)
                {
                    this.Add(collection.TypeArguments[0]);
                    this.registrations.Add((type, $"{Resolver}.RegisterCollection<{Name(collection.TypeArguments[0])}, {name}>();"));
                }
            }
        }

        private INamedTypeSymbol? FindScope(ITypeSymbol type)
        {
            IEnumerable<INamedTypeSymbol> Candidates(ITypeSymbol target)
            {
                if (target is IArrayTypeSymbol array)
                {
                    return Candidates(array.ElementType);
                }

                if (target is not INamedTypeSymbol named)
                {
                    return Enumerable.Empty<INamedTypeSymbol>();
                }

                return (named.ContainingType is { } parent ? new[] { parent }.Concat(Candidates(parent)) : Enumerable.Empty<INamedTypeSymbol>()).Concat(named.TypeArguments.SelectMany(Candidates));
            }

            return Candidates(type).FirstOrDefault(x => this.compilation.IsSymbolAccessibleWithin(type, x) &&
                SymbolEqualityComparer.Default.Equals(x.ContainingAssembly, this.compilation.Assembly) &&
                CanGenerateBridge(x));

            static bool CanGenerateBridge(INamedTypeSymbol scope)
            {
                for (var current = scope; current is not null; current = current.ContainingType)
                {
                    if (current.DeclaringSyntaxReferences.Length == 0 || !current.DeclaringSyntaxReferences.All(r => r.GetSyntax() is TypeDeclarationSyntax d && d.Modifiers.Any(SyntaxKind.PartialKeyword)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private string Bridge(INamedTypeSymbol scope, string code, string methodName, StringBuilder output)
        {
            var ancestors = new Stack<INamedTypeSymbol>();
            for (var type = scope; type is not null; type = type.ContainingType)
            {
                ancestors.Push(type);
            }

            var ns = scope.ContainingNamespace;
            if (!ns.IsGlobalNamespace)
            {
                output.Append("namespace ").Append(ns.ToDisplayString()).AppendLine(" {");
            }

            foreach (var type in ancestors)
            {
                var kind = type.IsRecord ? (type.IsValueType ? "record struct" : "record class") : type.TypeKind switch { TypeKind.Struct => "struct", TypeKind.Interface => "interface", _ => "class" };
                output.Append("partial ").Append(kind).Append(" @").Append(type.Name);
                if (type.Arity > 0)
                {
                    output.Append('<').Append(string.Join(", ", type.TypeParameters.Select(x => "@" + x.Name))).Append('>');
                }

                output.AppendLine(" {");
            }

            output.Append("internal static void ").Append(methodName).AppendLine("() {").AppendLine(code).AppendLine("}");
            for (var i = 0; i < ancestors.Count; i++)
            {
                output.AppendLine("}");
            }

            if (!ns.IsGlobalNamespace)
            {
                output.AppendLine("}");
            }

            var call = $"{Name(scope)}.{methodName}();";
            return this.compilation.IsSymbolAccessibleWithin(scope, this.compilation.Assembly) ? call : this.Bridge(scope.ContainingType!, call, methodName, output);
        }
    }
}
