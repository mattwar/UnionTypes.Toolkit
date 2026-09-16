using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionTypes.Toolkit.Generators;

public static class CommentPropertyExtensions
{
    /// <summary>
    /// Returns true if the comment property exists in the node's leading trivia.
    /// </summary>
    public static bool HasCommentProperty(this SyntaxNode node, string propertyName)
    {
        return TryGetCommentProperty(node, propertyName, out _);
    }

    /// <summary>
    /// Returns true if the comment property exists in the symbol's declaration node's leading trivia.
    /// </summary>
    public static bool HasCommentProperty(this ISymbol symbol, string propertyName)
    {
        return TryGetCommentProperty(symbol, propertyName, out _);
    }

    /// <summary>
    /// Returns true if the comment property exists in the node's leading trivia, and outputs properties assigned value if present.
    /// </summary>
    public static bool TryGetCommentProperty(this SyntaxNode node, string propertyName, out string? value)
    {
        value = null;
        var commentTrivia = node.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)).ToArray();
        foreach (var trivia in commentTrivia)
        {
            var text = trivia.ToString();
            var prefix = "@" + propertyName;
            var startIndex = text.IndexOf(prefix);
            if (startIndex >= 0)
            {
                var endOfPrefix = startIndex + prefix.Length;

                if (endOfPrefix < text.Length && text[endOfPrefix] == '=')
                {
                    startIndex = endOfPrefix + 1;
                    var endIndex = text.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }, startIndex);
                    if (endIndex < 0)
                        endIndex = text.Length;
                    value = text.Substring(startIndex, endIndex - startIndex);
                    return true;
                }
                else if (endOfPrefix == text.Length 
                    || text.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }, endOfPrefix) >= endOfPrefix)
                {
                    value = "true";
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if the comment property exists in the node's leading trivia, and outputs properties assigned value if present and convertible to the type T.
    /// </summary>
    public static bool TryGetCommentProperty<T>(this SyntaxNode node, string propertyName, out T? value)
    {
        value = default;
        if (TryGetCommentProperty(node, propertyName, out var strValue))
        {
            try
            {
                value = (T)Convert.ChangeType(strValue, typeof(T));
                return true;
            }
            catch
            {
                // ignore conversion errors and just return false
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if the comment property exists in the symbol's declaration node's leading trivia, and outputs the property's assigned value if present.
    /// </summary>
    public static bool TryGetCommentProperty(this ISymbol symbol, string propertyName, out string? value)
    {
        value = null;
        foreach (var node in symbol.GetDeclarationNodes())
        {
            if (TryGetCommentProperty(node, propertyName, out value))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if the comment property exists in the symbol's declaration node's leading trivia, and outputs the property's assigned value if present and convertible to the type T.
    /// </summary>
    public static bool TryGetCommentProperty<T>(this ISymbol symbol, string propertyName, out T? value)
    {
        value = default;
        foreach (var node in symbol.GetDeclarationNodes())
        {
            if (TryGetCommentProperty(node, propertyName, out value))
            {
                return true;
            }
        }
        return false;
    }
}