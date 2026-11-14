using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Tests;

public static class TestHelpers
{
    public static void AssertNoDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics.Length > 0)
        {
            Assert.Fail($"Unexpected diagnostic: {diagnostics[0]}");
        }
    }

    /// <summary>
    /// Converts text with marked ranges into unmarked text and the list of ranges
    /// </summary>
    public static (string unmarkedText, (int start, int length)[] markedRanges) GetMarkedRanges(string text, string rangeStart = "<<", string rangeEnd = ">>")
    {
        var markedRanges = new List<(int start, int length)>();
        var unmarkedText = new System.Text.StringBuilder();

        // gather all the marked ranges and build the unmarked text by removing the markers.
        var pos = 0;               
        var delta = 0; // keep track of delta to adjust to positions in the unmarked text
        while (true)
        {
            int openStart = text.IndexOf(rangeStart, pos);
            int closeStart = openStart > pos ? text.IndexOf(rangeEnd, openStart + rangeStart.Length) : -1;
            if (openStart > pos && closeStart > openStart)
            {
                // append everything before the marked range to the unmarked text
                unmarkedText.Append(text.Substring(pos, openStart - pos));

                // append everything between the start and end of the marked range to the unmarked text
                unmarkedText.Append(text.Substring(openStart + rangeStart.Length, closeStart - (openStart + rangeStart.Length)));

                // add range to list of marked ranges
                markedRanges.Add((openStart - delta, closeStart - (openStart + rangeStart.Length)));

                // account for the length of the start and end markers, so the ranges match the unmarked text.
                delta += rangeStart.Length + rangeEnd.Length;

                // advance position to after the end of the marked range
                pos = closeStart + rangeEnd.Length;
                continue;
            }
            else
            {
                // append rest of text after last marked range
                unmarkedText.Append(text.Substring(pos));
                break;
            }
        }

        return (unmarkedText.ToString(), markedRanges.ToArray());
    }   

    public static CSharpCompilation CreateCompilation(string[] sourceTexts, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: ["DEBUG"]);
        return CSharpCompilation.Create(
            assemblyName: "compilation",
            syntaxTrees: sourceTexts.Select((s, i) => CSharpSyntaxTree.ParseText(s, parseOptions, $"Text#{i+1}")).ToArray(),
            references: new[] { Core, Netstandard, SystemRuntime, SystemConsole },
            options: new CSharpCompilationOptions(outputKind)
                .WithNullableContextOptions(NullableContextOptions.Enable)
        );
    }

    public static CSharpCompilation CreateCompilation(string sourceText, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        return CreateCompilation([sourceText], outputKind);
    }

    public static object? Run(CSharpCompilation compilation, object?[]? args = null, string typeName = "Program", string methodName = "Main")
    {
        var assemblyStream = new MemoryStream();
        var result = compilation.Emit(assemblyStream);
        if (!result.Success)
        {
            throw new InvalidOperationException("Compilation failed");
        }
        assemblyStream.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(assemblyStream.ToArray());
        var type = assembly.GetType(typeName);
        var methods = type!.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var method = methods.FirstOrDefault(m => m.Name == methodName);
        if (method == null)
            method = methods.FirstOrDefault(m => m.Name == "<Main>$");
        var parameters = method!.GetParameters();
        if (args == null)
            args = new object?[parameters.Length];
        try
        {
            return method.Invoke(null, args);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public static string RuntimeDirectory = 
        Path.GetDirectoryName(typeof(Enumerable).GetTypeInfo().Assembly.Location)!;
    
    public static readonly MetadataReference Netstandard =
        MetadataReference.CreateFromFile(Path.Combine(RuntimeDirectory, "netstandard.dll"));

    public static readonly MetadataReference SystemRuntime =
        MetadataReference.CreateFromFile(Path.Combine(RuntimeDirectory, "System.Runtime.dll"));

    public static readonly MetadataReference Core =
        MetadataReference.CreateFromFile(typeof(int).GetTypeInfo().Assembly.Location);

    public static readonly MetadataReference SystemConsole =
        MetadataReference.CreateFromFile(Path.Combine(RuntimeDirectory, "System.Console.dll"));
}