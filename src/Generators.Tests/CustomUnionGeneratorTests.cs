using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using UnionTypes.Toolkit.Generators;

namespace Tests
{
    [TestClass]
    public class CustomUnionGeneratorTests
    {
        [TestMethod]
        public void UnionInfo_WithDifferentCaseCounts_AreNotEqual()
        {
            var threeCases = new UnionInfo(
                "TestUnion",
                [
                    new CaseDesc(TypeDesc.Int32),
                    new CaseDesc(TypeDesc.Float),
                    new CaseDesc(TypeDesc.Double)
                ]);
            var fourCases = new UnionInfo(
                "TestUnion",
                [
                    new CaseDesc(TypeDesc.Int32),
                    new CaseDesc(TypeDesc.Float),
                    new CaseDesc(TypeDesc.Double),
                    new CaseDesc(TypeDesc.Decimal)
                ]);

            Assert.IsFalse(threeCases.Equals(fourCases));
            Assert.IsFalse(fourCases.Equals(threeCases));
        }

        [TestMethod]
        public void TestOverlappablePrimitiveCases()
        {
            TestGenerate(
                new UnionInfo(
                    "TestUnion",
                    [
                        new CaseDesc(TypeDesc.Int32),
                        new CaseDesc(TypeDesc.Float),
                        new CaseDesc(TypeDesc.Double),
                        new CaseDesc(TypeDesc.Decimal),
                    ]),
                generatedText =>
                {
                    // prove that the two primitives got overlapped into the overlapped field
                    Assert.IsTrue(generatedText.Contains("int _kind"));                    
                    Assert.IsTrue(generatedText.Contains("_overlapped"));
                    Assert.IsFalse(generatedText.Contains("_value1"));

                    // check for overlapped case fields
                    Assert.IsTrue(generatedText.Contains("int Case1"));
                    Assert.IsTrue(generatedText.Contains("float Case2"));
                    Assert.IsTrue(generatedText.Contains("double Case3")); 
                    Assert.IsTrue(generatedText.Contains("decimal Case4"));
                }                    
                );
        }

        [TestMethod]
        public void TestOverlappableStructCases()
        {
            // prove that structs will be overlapped, if at least two cases request overlap
            TestGenerate(
                """
                public record struct A(int Value);
                public record struct B(float Value);
                """,
                new UnionInfo(
                    "TestUnion",
                    [
                        new CaseDesc(new TypeDesc("A", TypeDescKind.Struct, StorageKind.Overlap)),
                        new CaseDesc(new TypeDesc("B", TypeDescKind.Struct, StorageKind.Overlap))
                    ]
                    ),
                generatedText =>
                {
                    Assert.IsTrue(generatedText.Contains("int _kind"));                    
                    Assert.IsTrue(generatedText.Contains("_overlapped"));
                    Assert.IsFalse(generatedText.Contains("_value1"));
                    Assert.IsTrue(generatedText.Contains("A Case1"));
                    Assert.IsTrue(generatedText.Contains("B Case2"));
                }
                );
        }

        [TestMethod]
        public void TestOverlappableNonDecomposableCase_IsolatedAlternate()
        {
            // prove that if a non-decomposable type is the only case that overlaps, then it will be isolated instead of overlapped
            TestGenerate(
                """
                public record struct A(int Value);
                public record struct B(float Value);
                """,
                new UnionInfo(
                    "TestUnion",
                    [
                        new CaseDesc(new TypeDesc("string", TypeDescKind.Class)),
                        new CaseDesc(new TypeDesc("int", TypeDescKind.Primitive))
                    ]
                    ),
                generatedText =>
                {
                    Assert.IsFalse(generatedText.Contains("_overlapped"));
                    Assert.IsTrue(generatedText.Contains("object? _value1"));
                    Assert.IsTrue(generatedText.Contains("int _value2"));
                }
                );
        }

        [TestMethod]
        public void TestOverlappableDecomposableStructCase_DecomposableAlternate()
        {
            // prove that if B cannot be overlapped (since it is the only case that overlaps), then it will be decomposed instead
            TestGenerate(
                """
                public record struct A(int Value);
                public record struct B(float Value);
                """,
                new UnionInfo(
                    "TestUnion",
                    [
                        new CaseDesc(new TypeDesc("string", TypeDescKind.Class)),
                        new CaseDesc(new TypeDesc("B", TypeDescKind.Struct, StorageKind.Overlap, [
                            new MemberDesc("Value", TypeDesc.Float, isParameter: true)
                            ]))
                    ]
                    ),
                generatedText =>
                {
                    Assert.IsFalse(generatedText.Contains("_overlapped"));
                    Assert.IsTrue(generatedText.Contains("object? _value1"));
                    Assert.IsTrue(generatedText.Contains("float _value2"));
                }
                );
        }

        [TestMethod]
        public void TestOverlappableDecomposableStructCases()
        {
            TestGenerate(
                """
                public record struct A(int Value, string Value2);
                public record struct B(float Value, int Value2, string Value3);
                """,
                new UnionInfo(
                    "TestUnion",
                    [
                        new CaseDesc(new TypeDesc("A",
                            [
                                new MemberDesc("Value", TypeDesc.Int32, isParameter: true),
                                new MemberDesc("Value2", TypeDesc.String, isParameter: true)
                                ]
                            )),
                        new CaseDesc(new TypeDesc("B",
                            [
                                new MemberDesc("Value", TypeDesc.Float, isParameter: true),
                                new MemberDesc("Value2", TypeDesc.Int32, isParameter: true),
                                new MemberDesc("Value3", TypeDesc.String, isParameter: true)
                            ]))
                    ]
                    ),
                generatedText =>
                {
                    Assert.IsTrue(generatedText.Contains("int _kind"));                    
                    Assert.IsTrue(generatedText.Contains("_overlapped"));
                    Assert.IsTrue(generatedText.Contains("_value1"));
                    Assert.IsTrue(generatedText.Contains("int Case1"));
                    Assert.IsTrue(generatedText.Contains("(float, int) Case2"));
                }
                );
        }

        [TestMethod]
        public void TestDecomposableStructCases_MixedElements()
        {
            TestGenerate(
                """
                public record struct A(string Value);
                public record struct B(int Value);
                public record struct C(int Value, string Value2);
                public record struct D(string Value, int Value2);
                public record struct E(string Value, string Value2);
                public record struct F(int Value, int Value2);
                """,
                new UnionInfo(
                    "TestUnion",
                    [
                        new CaseDesc(new TypeDesc("A",
                            [
                                new MemberDesc("Value", TypeDesc.String, isParameter: true)
                            ]
                            )),
                        new CaseDesc(new TypeDesc("B",
                            [
                                new MemberDesc("Value", TypeDesc.Int32, isParameter: true)
                            ]
                            )),
                        new CaseDesc(new TypeDesc("C",
                            [
                                new MemberDesc("Value", TypeDesc.Int32, isParameter: true),
                                new MemberDesc("Value2", TypeDesc.String, isParameter: true)
                            ])),
                        new CaseDesc(new TypeDesc("D",
                            [
                                new MemberDesc("Value", TypeDesc.String, isParameter: true),
                                new MemberDesc("Value2", TypeDesc.Int32, isParameter: true)
                            ])),
                        new CaseDesc(new TypeDesc("E",
                            [
                                new MemberDesc("Value", TypeDesc.String, isParameter: true),
                                new MemberDesc("Value2", TypeDesc.String, isParameter: true)
                            ])),
                        new CaseDesc(new TypeDesc("F",
                            [
                                new MemberDesc("Value", TypeDesc.Int32, isParameter: true),
                                new MemberDesc("Value2", TypeDesc.Int32, isParameter: true)
                            ]))
                    ]
                    ),
                generatedText =>
                {
                    Assert.IsTrue(generatedText.Contains("int _kind"));                    
                    Assert.IsTrue(generatedText.Contains("_overlapped"));
                    Assert.IsTrue(generatedText.Contains("object? _value1"));
                    Assert.IsTrue(generatedText.Contains("object? _value2"));
                    Assert.IsTrue(generatedText.Contains("int Case2"));
                    Assert.IsTrue(generatedText.Contains("int Case3"));
                    Assert.IsTrue(generatedText.Contains("int Case4"));
                    Assert.IsTrue(generatedText.Contains("(int, int) Case6"));
                }
                );
        }


        [TestMethod]
        public void TestEmptyCases()
        {
            // prove we can decompose empty structs into 'nothing' and still retain the case
            TestGenerate(
                """
                public record struct A();
                public record struct B();
                """,
                new UnionInfo(
                    "TestUnion",
                    [
                        new CaseDesc(new TypeDesc("A", TypeDescKind.Struct, StorageKind.Decompose)),
                        new CaseDesc(new TypeDesc("B", TypeDescKind.Struct, StorageKind.Decompose))
                    ]
                    ),
                generatedText =>
                {
                    Assert.IsTrue(generatedText.Contains("int _kind"));                    
                    // there is no overlapped data so the field should not exist
                    Assert.IsFalse(generatedText.Contains("_overlapped"));
                    // there is no data other than the tag _kind, so no value fields should exist
                    Assert.IsFalse(generatedText.Contains("_value1"));
                }
                );
        }


        private void TestGenerate(UnionInfo union, Action<string>? generatedTextValidator = null)
        {
            TestGenerate([], union, generatedTextValidator);
        }

        private void TestGenerate(string additionalText, UnionInfo union, Action<string>? generatedTextValidator = null)
        {
            TestGenerate([additionalText], union, generatedTextValidator);
        }

        private void TestGenerate(string[] additionalTexts, UnionInfo union, Action<string>? generatedTextCheck = null)
        {
            var generator = new CustomUnionGenerator();
            var generatedText = generator.Generate(union);
            string[] allTexts = [..additionalTexts, generatedText];

            // prove that generated code (+ addition texts) compiles without errors or warnings
            var compilation = CreateCompilation(allTexts);

            var diagnostics = compilation.GetDiagnostics().Where(
                d => d.Severity == DiagnosticSeverity.Error
                    || d.Severity == DiagnosticSeverity.Warning)
                .ToImmutableArray();

            if (diagnostics.Length > 0)
            {
                Assert.Fail($"Unexpected diagnostic: {diagnostics[0]}");
            }

            generatedTextCheck?.Invoke(generatedText);
        }

        public static CSharpCompilation CreateCompilation(params string[] sources)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            return CSharpCompilation.Create(
                assemblyName: "compilation",
                syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions)).ToArray(),
                references: new[] { Core, Netstandard, SystemRuntime },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
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
}