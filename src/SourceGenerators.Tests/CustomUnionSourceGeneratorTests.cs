using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using UnionTypes.Toolkit;
using UnionTypes.Toolkit.Generators;

namespace Tests;

using static TestHelpers;

[TestClass]
public class CustomUnionSourceGeneratorTests
{
    [TestMethod]
    public void TestOverlappableCases_Primitives()
    {
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases(int case1, float case2);
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));
                Assert.IsFalse(HasValueFields(generatedText)); // no value fields
            });
    }

    [TestMethod]
    public void TestOverlappableCases_RecordStructs()
    {
        // record structs can be overlapped if they have only overlappable members (e.g. no reference types)
        // this can only happen if the record struct can be trusted (e.g. is defined in the same assembly as the union), otherwise it is not safe to overlap them.
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases(A case1, B case2);
            }

            public record struct A(int X);
            public record struct B { public float Y { get; init; } }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "global::A"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::B"));
            });
    }

    [TestMethod]
    public void TestOverlappableCases_ArbitraryStructs()
    {
        // arbitrary structs can be overlapped if they have only overlappable members (e.g. no reference types)
        // this can only happen if the struct can be trusted (e.g. is defined in the same assembly as the union), otherwise it is not safe to overlap them.
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases(A case1, B case2);
            }

            public struct A { public int X { get; init; }}
            public struct B { public float Y { get; init; } }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "global::A"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::B"));
            });
    }

    [TestMethod]
    public void TestOverlappableCases_Tuples()
    {
        // value tuples (structs) can be overlapped if they have only overlappable members (e.g. no reference types)
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases((int X, float Y) case1, (float X, int Y) case2);
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "(int X, float Y)"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(float X, int Y)"));
            });
    }

    [TestMethod]
    public void TestOverlappableCases_Enums()
    {
        // numeric enums can be overlapped because they are represented as a numeric primitive
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases(E case1, F case2);
            }

            public enum E { A, B, C }
            public enum F { X, Y, Z }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "global::E"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::F"));
            });
    }

    [TestMethod]
    public void TestDecomposableCases_RecordStructs()
    {
        // record structs that cannot be overlapped (because they have reference type members) can still be decomposed into their primitive members, of which some may be overlapped.
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases(A case1, B case2, C case3);
            }

            public record struct A(int Value1, string Value2);
            public record struct B { public required float Value { get; init; } public required string Value2 { get; init; } }
            public record struct C(decimal Value1) { public required string Value2 { get; init; } }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));  // int member of A can overlap
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));  // float member of B can overlap
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 3, "decimal"));  // decimal member of C can overlap 
                Assert.IsTrue(HasValueFields(generatedText, "object?")); // reference members go into reusable object fields
            });
    }

    [TestMethod]
    public void TestDecomposableCases_Tuples()
    {
        // tuples with non-overlappable members cannot be overlapped, but can be decomposed into their members, some of which may be overlapped.
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases((int X, string Y) case1, (string X, float Y) case2);
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));  // int member of case1 can overlap
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));  // float member of case2 can overlap
                Assert.IsTrue(HasValueFields(generatedText, "object?")); // reference members go into reusable object fields
            });
    }

    [TestMethod]
    public void TestDecomposableCases_MulitpleOverlappableMembers()
    {
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases(A case1, (float X, int Y, string Z) case2);
            }

            public record struct A(int Value1, float Value2, string Value3);
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "(int, float)"));  // members of case1 that can overlap as a tuple
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(float, int)"));  // members of case2 that can overlap as a tuple
                Assert.IsTrue(HasValueFields(generatedText, "object?")); // reference members go into reusable object fields
            });
    }

    [TestMethod]
    public void TestDecomposableCases_NestedDecomposableMembers()
    {
        // Decomposable case types can contain members that are themselves decomposable.
        // Each overlappable decomposed member is stored in the same overlapped field as a tuple of the overlappable members.
        TestGenerator(
            """
            public partial struct MyUnion
            {
                partial void Cases(A case1, B case2);
            }

            public record struct A(int Value1, (float, string) Value2);
            public record struct B(float Value1, (int, string) Value2);
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "(int, float)"));  // members of case1 that can overlap as a tuple
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(float, int)"));  // members of case2 that can overlap as a tuple
                Assert.IsTrue(HasValueFields(generatedText, "object?")); // reference members go into reusable object fields
            });
    }

    [TestMethod]
    public void TestCaseLayoutOverrides_Overlap()
    {
        // prove that w/o @overlap, the generator isolates the cases into separate fields
        TestGenerator(
            """
            using System;

            public partial struct MyUnion
            {
                partial void Cases(
                    int case1, 
                    DateOnly case2          // not known to be overlappable
                    );
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "int", "global::System.DateOnly"));
            });

        // prove that with the @overlap annotation they generator will overlap the case 
        TestGenerator(
            """
            using System;

            public partial struct MyUnion
            {
                partial void Cases(
                    int case1, 
                    // @overlap
                    DateOnly case2
                    );
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::System.DateOnly"));
                Assert.IsFalse(HasValueFields(generatedText)); 
            });
    }

    [TestMethod]
    public void TestCaseLayoutOverrides_Box()
    {
        // override normal overlap/decompose/isolate behavior to force a case to be store in an a shared object field.
        TestGenerator(
            """
            using System;

            public partial struct MyUnion
            {
                partial void Cases(
                    // @box
                    int case1, 
                    // @box
                    (int, float) case2
                    );
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            });
    }

    [TestMethod]
    public void TestCaseLayoutOverrides_Isolate()
    {
        // override normal overlap/decompose behavior and force to isolate this case
        TestGenerator(
            """
            using System;

            public partial struct MyUnion
            {
                partial void Cases(
                    int case1, 
                    // @isolate
                    float case2,
                    // @isolate
                    string case3
                    );
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "int", "float", "string"));
            });
    }

    [TestMethod]
    public void TestCaseLayoutOverrides_Decompose()
    {
        // override normal overlap behavior and force to decompose this case
        TestGenerator(
            """
            using System;

            public record struct A(int X, float Y);
            public record struct B { public int X { get; init; } public float Y { get; init; } }

            public partial struct MyUnion
            {
                partial void Cases(
                    // @decompose
                    A case1, 
                    // @decompose
                    B case2
                    );
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "(int, float)"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(int, float)"));
            });
    }

    [TestMethod]
    public void TestNonDisjointCases_Interfaces()
    {
        TestGenerator(
            """
            public interface IA { }
            public interface IB { }

            public partial struct MyUnion
            {
                partial void Cases(IA case1, IB case2);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::IB"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::IA"));
            });
    }

    [TestMethod]
    public void TestNonDisjointCases_InterfacesAndImplementingStruct()
    {
        TestGenerator(
            """
            public interface IA { }
            public struct B : IA { }
            public struct C { }

            public partial struct MyUnion
            {
                partial void Cases(IA case1, B case2, C case3);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::B"));    // struct B implements IA, so case 2 can be retrieved from case 1
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::IA"));   // struct B implements IA, so case 1 can be retrieved from case 2
                Assert.IsFalse(HasTryGetWhen(generatedText, 3, "global::C"));   // struct C does not implement IA
            });
    }

    [TestMethod]
    public void TestNonDisjointCases_InterfacesAndClasses()
    {
        TestGenerator(
            """
            public interface IA { }

            public struct B  { }         // known to not implement IA
            public class C { }           // not known to implement IA, but unsealed
            public sealed class D { }    // known to not implement IA

            public partial struct MyUnion
            {
                partial void Cases(IA case1, B case2, C case3, D case4);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasTryGetWhen(generatedText, 1, "global::B"));   // case A can never be a B
                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::C"));    // case A might be a C
                Assert.IsFalse(HasTryGetWhen(generatedText, 1, "global::D"));   // case A can never be a D

                Assert.IsFalse(HasTryGetWhen(generatedText, 2, "global::IA"));   // case B can never be an IA
                Assert.IsTrue(HasTryGetWhen(generatedText, 3, "global::IA"));   // case C might be an IA (via subtype)
                Assert.IsFalse(HasTryGetWhen(generatedText, 4, "global::IA"));   // case D can never be an IA
            });
    }

    [TestMethod]
    public void TestNonDisjointCases_AnythingAndTypeParameter()
    {
        TestGenerator(
            """
            public partial struct MyUnion<T>
            {
                partial void Cases(int case1, T case2);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "int", "T"));

                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "T"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "int"));
            });
    }

    [TestMethod]
    public void TestNonDisjointCases_Subclasses()
    {
        // subclasses are not disjoint because a value of a sub type can be stored as a case of the base type.

        TestGenerator(
            """
            public class A { }
            public class B : A { }
            public class C : B { }

            public partial struct MyUnion
            {
                partial void Cases(A case1, B case2, C case3);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));  // no overlappable case types types or decomposable cases with overlappable members
                Assert.IsTrue(HasValueFields(generatedText, "object?"));   // all three cases share the same field

                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::B"));    // B is a subclass of A
                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::C"));    // C is a subclass of A
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::A"));    // A is a superclass of B
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::C"));    // C is a subclass of B
                Assert.IsTrue(HasTryGetWhen(generatedText, 3, "global::A"));    // A is a superclass of C
                Assert.IsTrue(HasTryGetWhen(generatedText, 3, "global::B"));    // B is a superclass of C
            });
    }

    [TestMethod]
    public void TestInNamespace()
    {
        TestGenerator(
            """
            namespace MyNamespace
            {
                public partial struct MyUnion
                {
                    partial void Cases(int case1, string case2);
                }
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(HasNamespaceDeclaration(generatedText, "MyNamespace"));
                Assert.IsTrue(HasValueFields(generatedText, "int", "object?"));
            });
    }

    [TestMethod]
    public void TestUsings()
    {
        TestGenerator(
            """
            global using System.Runtime;
            using System.Collections.Generic;
            using X=System.Collections.Generic.List<int>;
    
            public partial struct MyUnion
            {
                partial void Cases(int case1, string case2);
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(generatedText.Contains("using System.Collections.Generic;")); // this one always exists
                Assert.IsFalse(generatedText.Contains("global using System.Runtime;"));  // extra usings not carried over
                Assert.IsFalse(generatedText.Contains("using X=System.Collections.Generic.List<int>;"));  // extra usings not carried over
            });
    }

    [TestMethod]
    public void TestCaseTypesInOtherNamespace()
    {
        TestGenerator(
            """
            namespace OtherNamespace
            {
                public struct A { public int X; }
                public struct B { public string Y; }
            }

            public partial struct MyUnion
            {
                partial void Cases(OtherNamespace.A case1, OtherNamespace.B case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasValueFields(generatedText, "global::OtherNamespace.A", "global::OtherNamespace.B"));
            });
    }

    [TestMethod]
    public void TestCaseTypesInsideOtherTypes()
    {
        TestGenerator(
            """
            public static class OtherType
            {
                public struct A { public int X; }
                public struct B { public string Y; }
            }

            public partial struct MyUnion
            {
                partial void Cases(OtherType.A case1, OtherType.B case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasValueFields(generatedText, "global::OtherType.A", "global::OtherType.B"));
            });
    }


    [TestMethod]
    public void TestInternalUnion()
    {
        TestGenerator(
            """
            internal partial struct MyUnion
            {
                partial void Cases(int case1, string case2);
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(generatedText.Contains("internal partial struct MyUnion"));
            });

        TestGenerator(
            """
            partial struct MyUnion
            {
                partial void Cases(int case1, string case2);
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(generatedText.Contains("internal partial struct MyUnion"));
            });
    }

    [TestMethod]
    public void TestInternalUnion_WithInternalCases()
    {
        TestGenerator(
            """
            internal struct A { }
            internal struct B { }

            internal partial struct MyUnion
            {
                partial void Cases(A case1, B case2);
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(generatedText.Contains("internal partial struct MyUnion"));
                Assert.IsTrue(generatedText.Contains("internal bool TryGetValue([NotNullWhen(true)] out global::A value)"));
                Assert.IsTrue(generatedText.Contains("internal bool TryGetValue([NotNullWhen(true)] out global::B value)"));
            });
    }

    [TestMethod]
    public void TestNullableCases_Primitives()
    {
        TestGenerator(
            """
            internal partial struct MyUnion
            {
                partial void Cases(int? case1, float? case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));
            });
    }

    [TestMethod]
    public void TestNullableCases_ExplicitNullableTypes()
    {
        TestGenerator(
            """
            internal partial struct MyUnion
            {
                partial void Cases(System.Nullable<int> case1, System.Nullable<float> case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));
            });
    }

    [TestMethod]
    public void TestNullableCases_ReferenceTypes()
    {
        TestGenerator(
            """
            public class A { }
            public class B { }

            internal partial struct MyUnion
            {
                partial void Cases(A? case1, B? case2);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            });
    }

    [TestMethod]
    public void TestNullableCases_OverlappableStructs()
    {
        TestGenerator(
            """
            public record struct A (int X, float Y);
            public record struct B { public required int X { get; init; } public required float Y { get; init; } }

            internal partial struct MyUnion
            {
                partial void Cases(A? case1, B? case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "global::A"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::B"));
                Assert.IsFalse(HasValueFields(generatedText));
            });
    }

    [TestMethod]
    public void TestNullableCases_DecomposableStructs()
    {
        TestGenerator(
            """
            public record struct A (int X, string Y);
            public record struct B { public required int X { get; init; } public required string Y { get; init; } }

            internal partial struct MyUnion
            {
                partial void Cases(A? case1, B? case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "int"));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            });
    }

    [TestMethod]
    public void TestNullableCaseMembers_DecomposableStructs()
    {
        TestGenerator(
            """
            public record struct A (int? X, string? Y);
            public record struct B { public int? X { get; init; } public string? Y { get; init; } public float? Z { get; init; } }

            internal partial struct MyUnion
            {
                partial void Cases(A case1, B case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int?"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(int?, float?)"));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            });
    }

    /// <summary>
    /// Returns true if the generated text contains a declaration for the overlapped field.
    /// </summary>
    private static bool HasOverlappedField(string text) =>
        text.Contains("_overlapped");

    /// <summary>
    /// Returns true if the generated text contains the expected value fields declarations in the order specified, and no others.
    /// </summary>
    private static bool HasValueFields(string text, params string[] fieldTypes)
    {
        // check for existence of each expected value field in order.
        for (int i = 0; i < fieldTypes.Length; i++)
        {
            var fieldType = fieldTypes[i];
            var test = $"{fieldType} _value{i + 1};";
            if (!text.Contains(test))
                return false;
        }

        // check that there is no extra value field beyond the expected ones.
        if (text.Contains($"_value{fieldTypes.Length + 1}"))
            return false;

        // if no fields are expected, return true if there are any value fields        
        if (fieldTypes.Length == 0)
            return text.Contains("_value");

        return true;
    }

    /// <summary>
    /// Returns true if the overlapped field type contains a field for the specified case index and type.
    /// </summary>
    private static bool HasOverlappedCaseField(string text, int caseIndex, string fieldType)
    {
        var test = $"{fieldType} Case{caseIndex}";
        return text.Contains(test);
    }

    /// <summary>
    /// Returns true if the generated text contains a TryGetValue method with the case xxx when yyy clause for the specified case index and type.
    /// </summary>
    private static bool HasTryGetWhen(string text, int otherCaseIndex, string caseType)
    {
        var test = $"case {otherCaseIndex} when this.GetCase{otherCaseIndex}() is {caseType} v:";
        return text.Contains(test);
    }

    /// <summary>
    /// Returns true if the generated text contains a namespace declaration for the specified namespace.
    /// </summary>
    private static bool HasNamespaceDeclaration(string text, string namespaceName)
    {
        var expected = $"namespace {namespaceName}";
        return text.Contains(expected);
    }

    /// <summary>
    /// Tests the <see cref="CustomUnionSourceGenerator"/> correctly generates code the partial union declaration in the sourceText,
    /// and combined with the source text produces a compilation with no errors or warnings.
    /// </summary>
    private void TestGenerator(string sourceText, Action<string>? generatedTextCheck = null)
    {
        var compilation = CreateCompilation(sourceText);
        var trees = compilation.SyntaxTrees.ToArray();

        var generator = new CustomUnionSourceGenerator();
        var parseOptions = trees[0].Options as CSharpParseOptions;
        var driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics).GetRunResult();

        Assert.IsTrue(result.GeneratedTrees.Length > 0, "no new files were generated");
        var newText = result.GeneratedTrees[0].ToString();

        var newDiagnostics = newCompilation.GetDiagnostics().Where(
            d => d.Severity == DiagnosticSeverity.Error
                || d.Severity == DiagnosticSeverity.Warning).ToImmutableArray();
        AssertNoDiagnostics(newDiagnostics, newText);

        generatedTextCheck?.Invoke(newText);
    }

#if false

    [TestMethod]
    public void TestIncremental_Unchanged()
    {
        // add a new class to the source file, should not cause a re-generation
        TestIncremental(
            """
            using UnionTypes.Toolkit;
            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            t => t,
            expectRegenerated: false
            );
    }

    [TestMethod]
    public void TestIncremental_UnrelatedChange()
    {
        // add a new class to the source file, should not cause a re-generation
        TestIncremental(
            """
            using UnionTypes.Toolkit;
            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            t => t.Append("public class OtherClass { }"),
            expectRegenerated: false
            );
    }

    [TestMethod]
    public void TestIncremental_WhitespaceChange()
    {
        // add a new class to the source file, should not cause a re-generation
        TestIncremental(
            """
            using UnionTypes.Toolkit;
            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            t => t.ReplaceOne("[TypeUnion]", "[TypeUnion]  "),
            expectRegenerated: false
            );
    }

    [TestMethod]
    public void TestIncremental_AddCase()
    {
        // add a new class to the source file, should not cause a re-generation
        TestIncremental(
            """
            using UnionTypes.Toolkit;
            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            t => t.InsertAfter("y);", "\n    public static partial MyUnion Create(double z);"),
            expectRegenerated: true
            );
    }

    [TestMethod]
    public void TestIncremental_ChangeOptions()
    {
        // add a new class to the source file, should not cause a re-generation
        TestIncremental(
            """
            using UnionTypes.Toolkit;
            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            t => t.ReplaceOne("[TypeUnion]", "[TypeUnion(GenerateToString=false)"),
            expectRegenerated: true
            );
    }

    private void TestIncremental(
        string sourceText, Func<SourceText, SourceText> fnChange, bool expectRegenerated)
    {
        var generator = new CustomUnionSourceGenerator().AsSourceGenerator();

        var options = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true
            );

        var driver = CSharpGeneratorDriver.Create([generator], driverOptions: options);

        var compilation = CreateCompilation(sourceText);
        var trees = compilation.SyntaxTrees.ToArray();
        var driverWithCache = driver.RunGenerators(compilation);
        var runResult = driverWithCache.GetRunResult();
        Assert.AreEqual(1, runResult.GeneratedTrees.Length);
        var runGenerated = !UsedCachedOutput(runResult, CustomUnionSourceGenerator.GenerateStepName);
        Assert.IsTrue(runGenerated);

        // clone w/o changing anything and run again; should not regenerate.
        var clonedCompilation = compilation.Clone();
        var clonedResult = driverWithCache.RunGenerators(clonedCompilation).GetRunResult();
        Assert.AreEqual(1, clonedResult.GeneratedTrees.Length);
        var cloneNotGenerated = UsedCachedOutput(clonedResult, CustomUnionSourceGenerator.GenerateStepName);
        Assert.IsTrue(cloneNotGenerated);

        // make change to an unrelated source file that should not cause a change in generated file
        var oldTree = trees[0];
        var oldText = oldTree.GetText();
        var changedText = fnChange(oldText);
        var changedTree = trees[0].WithChangedText(changedText);
        var changedCompilation = clonedCompilation.ReplaceSyntaxTree(oldTree, changedTree);

        // re-run generators after change
        var changedResult = driverWithCache.RunGenerators(changedCompilation).GetRunResult();
        Assert.AreEqual(1, changedResult.GeneratedTrees.Length);
        var changeRegenerated = !UsedCachedOutput(changedResult, CustomUnionSourceGenerator.GenerateStepName);
        Assert.AreEqual(expectRegenerated, changeRegenerated);
    }

    private static bool UsedCachedOutput(GeneratorDriverRunResult result, string stepName)
    {
        var cached = result.Results[0].TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Any(x => x.Reason == IncrementalStepRunReason.Cached);
        return cached;
    }

    public static HashSet<string> GetDataFields(string generatedText)
    {
        return GetIdentifiers("_data", generatedText);
    }

    public static HashSet<string> GetIdentifiers(string namePrefix, string generatedText)
    {
        var names = new HashSet<string>();
        var index = 0;
        while (index >= 0)
        {
            var foundIndex = generatedText.IndexOf(namePrefix, index);
            if (foundIndex < index)
                break;

            if (foundIndex > 0 && IsIdentifierChar(generatedText[foundIndex - 1]))
            {
                // middle of another identifier? skip forward
                index = foundIndex + 1;
            }
            else
            {
                var name = GetIdentifierAt(foundIndex);
                names.Add(name);
                index = foundIndex + name.Length;
            }
        }

        return names;

        string GetIdentifierAt(int start)
        {
            var end = start;
            
            while (end < generatedText.Length 
                && IsIdentifierChar(generatedText[end]))
            {
                end++;
            }

            return generatedText.Substring(start, end - start);
        }

        bool IsIdentifierChar(char ch) =>
            char.IsLetterOrDigit(ch) || ch == '_';
    }

#endif
}
