using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Tests;

using static TestHelpers;

public static class GeneratorTestHelpers
{
    /// <summary>
    /// Runs the generator over the declaration text and verifies the output.
    /// </summary>
    /// <param name="declarationText">The source text containing the declaration that triggers the generator.</param>
    /// <param name="generatedTextValidator">The optional action that will be called to validate the correctness of the generated text.</param>
    public static void TestGenerator<TGenerator>(
        string declarationText, 
        Action<string> generatedTextValidator)
        where TGenerator : IIncrementalGenerator, new()
    {
        Test<TGenerator>(declarationText, generatedTextValidator, null, null);
    }

    /// <summary>
    /// Runs the generator over the declaration text and verifies the output.
    /// </summary>
    /// <param name="declarationText">The source text containing the declaration that triggers the generator.</param>
    /// <param name="semanticValidator">The optional text of the C# code that runs to validate the correctness of the generated code.</param>
    public static void TestGenerator<TGenerator>(
        string declarationText, 
        string semanticValidator)
        where TGenerator : IIncrementalGenerator, new()
    {
        Test<TGenerator>(declarationText, null, null, semanticValidator);
    }

    /// <summary>
    /// Runs the generator over the declaration text and verifies the output.
    /// </summary>
    /// <param name="declarationText">The source text containing the declaration that triggers the generator.</param>
    /// <param name="generatedTextValidator">The optional action that will be called to validate the correctness of the generated text.</param>
    /// <param name="semanticValidator">The optional text of the C# code that runs to validate the correctness of the generated code.</param>
    public static void TestGenerator<TGenerator>(
        string declarationText,
        Action<string> generatedTextValidator,
        string semanticValidator)
        where TGenerator : IIncrementalGenerator, new()
    {
        Test<TGenerator>(declarationText, generatedTextValidator, null, semanticValidator);
    }

    /// <summary>
    /// Runs the generator over the declaration text and verifies the output.
    /// </summary>
    /// <param name="markedDeclarationText">The source text containing the declaration that triggers the generator, and any expected diagnostic ranges.</param>
    /// <param name="expectedDiagnosticCodes">The list of diagnostics the generator is expected to produce.</param>
    public static void TestGenerator<TGenerator>(
        string markedDeclarationText, 
        string[] expectedDiagnosticCodes)
        where TGenerator : IIncrementalGenerator, new()
    {
        Test<TGenerator>(markedDeclarationText, null, expectedDiagnosticCodes, null);
    }

    /// <summary>
    /// Runs the generator over the declaration text and verifies the output.
    /// </summary>
    /// <param name="markedDeclarationText">The source text containing the declaration that triggers the generator, and any expected diagnostic ranges.</param>
    /// <param name="generatedTextValidator">The optional action that will be called to validate the correctness of the generated text.</param>
    /// <param name="expectedDiagnosticCodes">The list of diagnostics the generator is expected to produce.</param>
    /// <param name="semanticValidator">The text of optional code that runs to validate the correctness of the generated code.</param>
    private static void Test<TGenerator>(
        string markedDeclarationText, 
        Action<string>? generatedTextValidator, 
        string[]? expectedDiagnosticCodes, 
        string? semanticValidator)
        where TGenerator : IIncrementalGenerator, new()
    {
        var (unmarkedText, markedRanges) = GetMarkedRanges(markedDeclarationText);

        var compilation = semanticValidator != null
            ? CreateCompilation([unmarkedText, semanticValidator], OutputKind.ConsoleApplication)
            : CreateCompilation([unmarkedText], OutputKind.DynamicallyLinkedLibrary);
        var trees = compilation.SyntaxTrees.ToArray();

        // run the generator
        var generator = new TGenerator();
        var parseOptions = trees[0].Options as CSharpParseOptions;
        var driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var generatorDiagnostics).GetRunResult();
       
        // verify generated diagnostics match expected diagnostics
        var actualGeneratorDiagnostics = generatorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning).ToArray();
        if (expectedDiagnosticCodes != null && markedRanges.Length != expectedDiagnosticCodes?.Length)
        {
            Assert.Fail($"there were {expectedDiagnosticCodes?.Length} expected diagnostics, but {markedRanges.Length} marked ranges in source text for the test.");
        }
        else if (actualGeneratorDiagnostics.Length != markedRanges.Length)
        {
            if (markedRanges.Length == 0)
            {
                var messages = string.Join("\r\n", actualGeneratorDiagnostics.Select(d => GetDiagnosticInfo(d)));
                Assert.Fail($"Unexpected diagnostics: {actualGeneratorDiagnostics.Length}\r\n{messages}");
            }
            else
            {
                Assert.Fail($"expected {markedRanges.Length} diagnostics, but got {actualGeneratorDiagnostics.Length}");               
            }
        }
        else if (actualGeneratorDiagnostics.Length > 0)
        {
            for (int i = 0; i < actualGeneratorDiagnostics.Length; i++)
            {
                var actual = actualGeneratorDiagnostics[i];

                if (expectedDiagnosticCodes != null && actual.Id != expectedDiagnosticCodes![i])
                {
                    Assert.Fail($"diagnostic {i} was expected to have code {expectedDiagnosticCodes[i]}, but was {actual.Id}");
                }

                var expectedRange = markedRanges[i];
                var actualRange = actual.Location.SourceSpan;

                if (actualRange.Start != expectedRange.start || actualRange.Length != expectedRange.length)
                {
                    Assert.Fail($"diagnostic {i} was expected at {expectedRange.start}:{expectedRange.length}, but was at {actualRange.Start}:{actualRange.Length}");
                }
            }
        }
        else // no actual generator diagnostics and no marked ranges
        {
            // verify there is exactly one generated document/tree
            if (result.GeneratedTrees.Length == 1)
            {
                // validate generated text
                var newText = result.GeneratedTrees[0].GetText().ToString();               
                generatedTextValidator?.Invoke(newText);
            }
            else 
            {
                Assert.Fail($"expected 1 generated tree, but got {result.GeneratedTrees.Length}");
            }

            // verify no compilation diagnostics
            var compilationDiagnostics = newCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning).ToArray();
            if (compilationDiagnostics.Length > 0)
            {
                var diagnosticMessages = string.Join(Environment.NewLine, compilationDiagnostics.Select(GetDiagnosticInfo));
                Assert.Fail($"Compilation produced unexpected diagnostics:{Environment.NewLine}{diagnosticMessages}");
            }

            // do semantic validation if validationText is provided and there are no generator diagnostics
            if (semanticValidator != null && newCompilation is CSharpCompilation runComp && actualGeneratorDiagnostics.Length == 0)
            {
                try
                {
                    TestHelpers.Run(runComp);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Execution of validation-text failed: {ex.Message}");
                }
            }
        }
    }

    private static string GetDiagnosticInfo(Diagnostic d)
    {
        if (d.Location.IsInSource)
        {
            var span = d.Location.SourceSpan;
            var tree = d.Location.SourceTree;
            var lineSpan = tree.GetLineSpan(span);
            return $"{d.Severity}: {lineSpan.Path}: ({lineSpan.StartLinePosition.Line}, {lineSpan.StartLinePosition.Character}: {span.Length}): {d.GetMessage()}";
        }
        return $"({d.Location}): {d.GetMessage()}";
    }
}