using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        
        foreach (var trivia in node.GetLeadingTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {                
                var text = trivia.ToString();
                var prefix = "@" + propertyName;
                var startIndex = text.IndexOf(prefix);
                if (startIndex >= 0)
                {
                    var endOfPrefix = startIndex + prefix.Length;

                    if (endOfPrefix < text.Length && text[endOfPrefix] == '=')
                    {
                        // determine value after =
                        startIndex = endOfPrefix + 1;
                        var endIndex = text.IndexOfAny(_commentPropertyEndingTokens, startIndex);
                        if (endIndex < 0)
                            endIndex = text.Length;
                        value = text.Substring(startIndex, endIndex - startIndex);
                        return true;
                    }
                    else if (endOfPrefix == text.Length 
                        || text.IndexOfAny(_commentPropertyEndingTokens, endOfPrefix) >= endOfPrefix)
                    {
                        // property without a value is considered to be "true"
                        value = "true";
                        return true;
                    }
                }
            }              
        }

        return false;
    }

    /// <summary>
    /// Any of these characters denotes the end of a comment property
    /// </summary>
    private static readonly char[] _commentPropertyEndingTokens = new[] { ' ', '\t', '\r', '\n', ',', ';', '|', ':', '(', ')', '[', ']', '{', '}' };

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