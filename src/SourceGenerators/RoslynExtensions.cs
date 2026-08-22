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
    }
}
