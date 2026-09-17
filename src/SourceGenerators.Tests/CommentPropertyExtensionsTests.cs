using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnionTypes.Toolkit.Generators;

namespace Tests;

[TestClass]
public class CommentPropertyExtensionsTests
{
    [TestMethod]
    public void TryGetCommentProperty_BareProperty_ReturnsTrueValue()
    {
        var declaration = GetClassDeclaration("// @union\nclass Example { }");

        var found = declaration.TryGetCommentProperty("union", out var value);

        Assert.IsTrue(found);
        Assert.AreEqual("true", value);
        Assert.IsTrue(declaration.HasCommentProperty("union"));
    }

    [TestMethod]
    public void TryGetCommentProperty_ValueStopsAtCommentDelimiters()
    {
        var declaration = GetClassDeclaration("/* @name=example, @enabled */ class Example { }");

        var found = declaration.TryGetCommentProperty("name", out var value);

        Assert.IsTrue(found);
        Assert.AreEqual("example", value);
        Assert.IsTrue(declaration.HasCommentProperty("enabled"));
    }

    [TestMethod]
    public void TryGetCommentProperty_MissingProperty_ReturnsFalse()
    {
        var declaration = GetClassDeclaration("// an unrelated comment\nclass Example { }");

        var found = declaration.TryGetCommentProperty("union", out var value);

        Assert.IsFalse(found);
        Assert.IsNull(value);
        Assert.IsFalse(declaration.HasCommentProperty("union"));
    }

    [TestMethod]
    public void TryGetCommentProperty_Generic_ConvertsValueAndRejectsInvalidValue()
    {
        var declaration = GetClassDeclaration("// @count=42 @enabled\nclass Example { }");

        var countFound = declaration.TryGetCommentProperty<int>("count", out var count);
        var enabledFound = declaration.TryGetCommentProperty<bool>("enabled", out var enabled);
        var invalidFound = declaration.TryGetCommentProperty<int>("enabled", out var invalid);

        Assert.IsTrue(countFound);
        Assert.AreEqual(42, count);
        Assert.IsTrue(enabledFound);
        Assert.IsTrue(enabled);
        Assert.IsFalse(invalidFound);
        Assert.AreEqual(0, invalid);
    }

    [TestMethod]
    public void TryGetCommentProperty_Symbol_SearchesAllDeclarationNodes()
    {
        var compilation = TestHelpers.CreateCompilation(
            "partial class Example { }",
            "// @union\npartial class Example { }");
        var symbol = compilation.GetTypeByMetadataName("Example");

        Assert.IsNotNull(symbol);
        var found = symbol!.TryGetCommentProperty("union", out var value);

        Assert.IsTrue(found);
        Assert.AreEqual("true", value);
        Assert.IsTrue(symbol.HasCommentProperty("union"));
    }

    [TestMethod]
    public void TryGetCommentProperty_GenericSymbol_ConvertsDeclarationValue()
    {
        var compilation = TestHelpers.CreateCompilation("// @priority=7\nclass Example { }");
        var symbol = compilation.GetTypeByMetadataName("Example");

        Assert.IsNotNull(symbol);
        var found = symbol!.TryGetCommentProperty<int>("priority", out var priority);

        Assert.IsTrue(found);
        Assert.AreEqual(7, priority);
    }

    private static ClassDeclarationSyntax GetClassDeclaration(string source)
    {
        return CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single();
    }
}