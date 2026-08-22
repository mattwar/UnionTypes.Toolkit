using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Tests;

public static class TestHelpers
{
    public static void AssertNoDiagnostics(ImmutableArray<Diagnostic> diagnostics, string newText)
    {
        if (diagnostics.Length > 0)
        {
            Assert.Fail($"Unexpected diagnostic: {diagnostics[0]}");
        }
    }

    public static CSharpCompilation CreateCompilation(params string[] sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        return CSharpCompilation.Create(
            assemblyName: "compilation",
            syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions)).ToArray(),
            references: new[] { Core, Netstandard, SystemRuntime },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable)
        );
    }

    public static string RuntimeDirectory = 
        Path.GetDirectoryName(typeof(Enumerable).GetTypeInfo().Assembly.Location)!;
    
    public static readonly MetadataReference Netstandard =
        MetadataReference.CreateFromFile(Path.Combine(RuntimeDirectory, "netstandard.dll"));

    public static readonly MetadataReference SystemRuntime =
        MetadataReference.CreateFromFile(Path.Combine(RuntimeDirectory, "System.Runtime.dll"));

    public static readonly MetadataReference Core =
        MetadataReference.CreateFromFile(typeof(int).GetTypeInfo().Assembly.Location);
}