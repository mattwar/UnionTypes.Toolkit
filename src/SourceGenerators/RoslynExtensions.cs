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
    internal static class RoslynExtensions
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
    }
}
