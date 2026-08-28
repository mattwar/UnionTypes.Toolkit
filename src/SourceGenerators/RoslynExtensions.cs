using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionTypes.Toolkit.Generators
{
    public static class RoslynExtensions
    {
        public static bool IsDeclaredInSource(this ITypeSymbol symbol)
        {
            return symbol.Locations.Any(loc => loc.IsInSource);
        }

        public static bool TryGetAttribute(this ISymbol symbol, string attributeName, out AttributeData attribute)
        {
            attribute = symbol.GetAttributes(attributeName).FirstOrDefault()!;
            return attribute != null;
        }

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, string attributeName)
        {
            if (!attributeName.EndsWith("Attribute"))
                attributeName += "Attribute";

            return symbol.GetAttributes().Where(symbol => symbol.AttributeClass?.Name == attributeName)!;
        }

        public static bool TryGetConstructorArgument(this AttributeData attribute, int position, out TypedConstant argument)
        {
            if (attribute.ConstructorArguments.Length > position)
            {
                argument = attribute.ConstructorArguments[position];
                return true;
            }

            argument = default;
            return false;
        }

        public static bool TryGetNamedArgument(this AttributeData attribute, string name, out TypedConstant argument)
        {
            if (attribute.NamedArguments.Any(na => na.Key == name))
            {
                argument = attribute.NamedArguments.First(na => na.Key == name).Value;
                return true;
            }

            argument = default;
            return false;
        }

        /// <summary>
        /// True if the type is a nullable type (either a Nullable<T> or a reference type with nullable annotation).
        /// </summary>
        public static bool IsNullable(this ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
                return true;

            return type.NullableAnnotation == NullableAnnotation.Annotated;
        }

        /// <summary>
        /// Returns the non-nullable version of the type. 
        /// If the type is a Nullable<T>, returns T. 
        /// If the type is a reference type with nullable annotation, returns the same type with NotAnnotated. 
        /// Otherwise, returns the original type.
        /// </summary>
        public static ITypeSymbol GetNonNullableType(this ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nt)
                return nt.TypeArguments[0];

            if (type.NullableAnnotation == NullableAnnotation.Annotated)
                return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

            return type;
        }

        public static IMethodSymbol? GetRecordPrimaryConstructor(this INamedTypeSymbol recordSymbol)
        {
            if (!recordSymbol.IsRecord)
                return null;

            // 1. If declared in source, use DeclaringSyntaxReferences
            var sourceCtor = recordSymbol.InstanceConstructors
                .FirstOrDefault(ctor => ctor.DeclaringSyntaxReferences
                    .Any(r => r.GetSyntax() is TypeDeclarationSyntax));

            if (sourceCtor != null)
                return sourceCtor;

            // 2. If imported from metadata:
            // Exclude the compiler-generated copy constructor (takes single parameter of the record type itself)
            var candidates = recordSymbol.InstanceConstructors
                .Where(ctor => !IsCopyConstructor(ctor, recordSymbol))
                .ToList();

            if (candidates.Count == 0)
                return null;

            if (candidates.Count == 1)
                return candidates[0];

            // Disambiguate using the compiler-generated Deconstruct method
            var deconstruct = recordSymbol.GetMembers("Deconstruct")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => !m.IsStatic);

            if (deconstruct != null)
            {
                var match = candidates.FirstOrDefault(ctor =>
                    ctor.Parameters.Length == deconstruct.Parameters.Length 
                    && ctor.Parameters
                        .Zip(deconstruct.Parameters, 
                            (cp, dp) =>
                                SymbolEqualityComparer.Default.Equals(cp.Type, dp.Type) &&
                                string.Equals(cp.Name, dp.Name, StringComparison.OrdinalIgnoreCase))
                        .All(m => m));

                if (match != null)
                    return match;
            }

            // Fallback: match parameter names/types to init-only positional properties
            var properties = recordSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && p.SetMethod != null && p.SetMethod.IsInitOnly)
                .ToList();

            return candidates.FirstOrDefault(ctor =>
                ctor.Parameters.Length <= properties.Count 
                && ctor.Parameters.All(param => 
                    properties.Any(prop =>
                        string.Equals(prop.Name, param.Name, StringComparison.OrdinalIgnoreCase)
                        && SymbolEqualityComparer.Default.Equals(prop.Type, param.Type)
                        )));
        }

        private static bool IsCopyConstructor(IMethodSymbol ctor, INamedTypeSymbol recordSymbol)
        {
            return ctor.Parameters.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(ctor.Parameters[0].Type, recordSymbol);
        }        
    }
}
