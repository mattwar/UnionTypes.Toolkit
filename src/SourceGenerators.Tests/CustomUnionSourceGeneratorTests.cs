using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using UnionTypes.Toolkit;
using UnionTypes.Toolkit.Generators;

namespace Tests;

using static TestHelpers;
using static GeneratorTestHelpers;

[TestClass]
public class CustomUnionSourceGeneratorTests
{
    [TestMethod]
    public void TestCaseMethod_BackwardCompat()
    {
        TestGenerator<CustomUnionSourceGenerator>(
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
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = 10;
            Debug.Assert(u1 is int i && i == 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is int iv && iv == 10);
            Debug.Assert(u1.TryGetValue(out int tgi) && tgi == 10);

            MyUnion u2 = 10.0f;
            Debug.Assert(u2 is float f && f == 10.0f);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is float fv && fv == 10.0f);
            Debug.Assert(u2.TryGetValue(out float tgf) && tgf == 10.0f);
            """
            );
    }

    [TestMethod]
    public void TestOverlappableCases_Primitives()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(int value);
                public partial MyUnion(float value);
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));
                Assert.IsFalse(HasValueFields(generatedText)); // no value fields
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = 10;
            Debug.Assert(u1 is int i && i == 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is int iv && iv == 10);
            Debug.Assert(u1.TryGetValue(out int tgi) && tgi == 10);

            MyUnion u2 = 10.0f;
            Debug.Assert(u2 is float f && f == 10.0f);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is float fv && fv == 10.0f);
            Debug.Assert(u2.TryGetValue(out float tgf) && tgf == 10.0f);
            """
            );
    }

    [TestMethod]
    public void TestOverlappableCases_RecordStructs()
    {
        // record structs can be overlapped if they have only overlappable members (e.g. no reference types)
        // this can only happen if the record struct can be trusted (e.g. is defined in the same assembly as the union), otherwise it is not safe to overlap them.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion(B value);
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
            },
            """
            using System.Diagnostics;

            MyUnion u1 = new A(10);
            Debug.Assert(u1 is A a && a.X == 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A av && av.X == 10);
            Debug.Assert(u1.TryGetValue(out A tga) && tga.X == 10);

            MyUnion u2 = new B { Y = 30.0f };
            Debug.Assert(u2 is B b && b.Y == 30.0f);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B bv && bv.Y == 30.0f);
            Debug.Assert(u2.TryGetValue(out B tgb) && tgb.Y == 30.0f);
            """
            );
    }

    [TestMethod]
    public void TestOverlappableCases_ArbitraryStructs()
    {
        // arbitrary structs can be overlapped if they have only overlappable members (e.g. no reference types)
        // this can only happen if the struct can be trusted (e.g. is defined in the same assembly as the union), otherwise it is not safe to overlap them.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion(B value);
            }

            public struct A { public int X { get; init; } }
            public struct B { public float Y { get; init; } }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "global::A"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::B"));
            },
            """
            using System.Diagnostics;

            MyUnion u1 = new A { X = 10 };
            Debug.Assert(u1 is A a && a.X == 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A av && av.X == 10);
            Debug.Assert(u1.TryGetValue(out A tga) && tga.X == 10);

            MyUnion u2 = new B { Y = 30.0f };
            Debug.Assert(u2 is B b && b.Y == 30.0f);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B bv && bv.Y == 30.0f);
            Debug.Assert(u2.TryGetValue(out B tgb) && tgb.Y == 30.0f);
            """
            );
    }

    [TestMethod]
    public void TestOverlappableCases_Tuples()
    {
        // value tuples (structs) can be overlapped if they have only overlappable members (e.g. no reference types)
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion((int X, float Y) value);
                public partial MyUnion((float X, int Y) value);
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "(int X, float Y)"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(float X, int Y)"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = (X: 10, Y: 30.0f); 
            Debug.Assert(u1 is ValueTuple<int, float> t1 && t1.Item1 == 10 && t1.Item2 == 30.0f);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is ValueTuple<int, float> t1v && t1v.Item1 == 10 && t1v.Item2 == 30.0f);
            Debug.Assert(u1.TryGetValue(out ValueTuple<int, float> t1tg) && t1tg.Item1 == 10 && t1tg.Item2 == 30.0f);

            MyUnion u2 = (X: 30.0f, Y: 10);
            Debug.Assert(u2 is ValueTuple<float, int> t2 && t2.Item1 == 30.0f && t2.Item2 == 10);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is ValueTuple<float, int> t2v && t2v.Item1 == 30.0f && t2v.Item2 == 10);
            Debug.Assert(u2.TryGetValue(out ValueTuple<float, int> t2tg) && t2tg.Item1 == 30.0f && t2tg.Item2 == 10);
            """
            );
    }

    [TestMethod]
    public void TestOverlappableCases_Enums()
    {
        // numeric enums can be overlapped because they are represented as a numeric primitive
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(E value);
                public partial MyUnion(F value);
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
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = E.A;
            Debug.Assert(u1 is E e && e == E.A);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is E ev && ev == E.A);
            Debug.Assert(u1.TryGetValue(out E t1tg) && t1tg == E.A);

            MyUnion u2 = F.X;
            Debug.Assert(u2 is F f && f == F.X);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is F fv && fv == F.X);
            Debug.Assert(u2.TryGetValue(out F t2tg) && t2tg == F.X);
            """
            );
    }

    [TestMethod]
    public void TestDecomposableCases_RecordStructs()
    {
        // record structs that cannot be overlapped (because they have reference type members) can still be decomposed into their primitive members, of which some may be overlapped.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion(B value);
                public partial MyUnion(C value);
            }

            public record struct A(int Value1, string Value2);
            public record struct B { public required float Value1 { get; init; } public required string Value2 { get; init; } }
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
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A(10, "ten");            
            Debug.Assert(u1 is A a && a.Value1 == 10 && a.Value2 == "ten");
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A av && av.Value1 == 10 && av.Value2 == "ten");
            Debug.Assert(u1.TryGetValue(out A t1tg) && t1tg.Value1 == 10 && t1tg.Value2 == "ten");

            MyUnion u2 = new B { Value1 = 30.0f, Value2 = "thirty" };
            Debug.Assert(u2 is B b && b.Value1 == 30.0f && b.Value2 == "thirty");
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B bv && bv.Value1 == 30.0f && bv.Value2 == "thirty");
            Debug.Assert(u2.TryGetValue(out B t2tg) && t2tg.Value1 == 30.0f && t2tg.Value2 == "thirty");

            MyUnion u3 = new C(50m) { Value2 = "fifty" };
            Debug.Assert(u3 is C c && c.Value1 == 50m && c.Value2 == "fifty");
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is C cv && cv.Value1 == 50m && cv.Value2 == "fifty");
            Debug.Assert(u3.TryGetValue(out C t3tg) && t3tg.Value1 == 50m && t3tg.Value2 == "fifty");
            """
            );
    }

    [TestMethod]
    public void TestDecomposableCases_Tuples()
    {
        // tuples with non-overlappable members cannot be overlapped, but can be decomposed into their members, some of which may be overlapped.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion((int X, string Y) value);
                public partial MyUnion((string X, float Y) value);
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));  // int member of case1 can overlap
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));  // float member of case2 can overlap
                Assert.IsTrue(HasValueFields(generatedText, "object?")); // reference members go into reusable object fields
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = (X: 10, Y: "ten");
            Debug.Assert(u1 is ValueTuple<int, string> t1 && t1.Item1 == 10 && t1.Item2 == "ten");
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is ValueTuple<int, string> tv1 && tv1.Item1 == 10 && tv1.Item2 == "ten");
            Debug.Assert(u1.TryGetValue(out ValueTuple<int, string> t1tg) && t1tg.Item1 == 10 && t1tg.Item2 == "ten");

            MyUnion u2 = (X: "twenty", Y: 20.0f);
            Debug.Assert(u2 is ValueTuple<string, float> t2 && t2.Item1 == "twenty" && t2.Item2 == 20.0f);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is ValueTuple<string, float> tv2 && tv2.Item1 == "twenty" && tv2.Item2 == 20.0f);
            Debug.Assert(u2.TryGetValue(out ValueTuple<string, float> t2tg) && t2tg.Item1 == "twenty" && t2tg.Item2 == 20.0f);
            """
            );
    }

    [TestMethod]
    public void TestDecomposableCases_MulitpleOverlappableMembers()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion((float X, int Y, string Z) value);
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
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A(10, 20f, "thirty");
            Debug.Assert(u1 is A a && a.Value1 == 10 && a.Value2 == 20f && a.Value3 == "thirty");
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A va && va.Value1 == 10 && va.Value2 == 20f && va.Value3 == "thirty");
            Debug.Assert(u1.TryGetValue(out A t1tg) && t1tg.Value1 == 10 && t1tg.Value2 == 20f && t1tg.Value3 == "thirty");

            MyUnion u2 = (X: 10f, Y: 20, Z: "thirty");
            Debug.Assert(u2 is ValueTuple<float, int, string> t2 && t2.Item1 == 10f && t2.Item2 == 20 && t2.Item3 == "thirty");
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is ValueTuple<float, int, string> tv2 && tv2.Item1 == 10f && tv2.Item2 == 20 && tv2.Item3 == "thirty");
            Debug.Assert(u2.TryGetValue(out ValueTuple<float, int, string> t2tg) && t2tg.Item1 == 10f && t2tg.Item2 == 20 && t2tg.Item3 == "thirty");
            """
            );
    }

    [TestMethod]
    public void TestDecomposableCases_NestedDecomposableMembers()
    {
        // Decomposable case types can contain members that are themselves decomposable.
        // Each overlappable decomposed member is stored in the same overlapped field as a tuple of the overlappable members.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion(B value);
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
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A(10, (20f, "thirty"));
            Debug.Assert(u1 is A a && a.Value1 == 10 && a.Value2.Item1 == 20f && a.Value2.Item2 == "thirty");
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A va && va.Value1 == 10 && va.Value2.Item1 == 20f && va.Value2.Item2 == "thirty");
            Debug.Assert(u1.TryGetValue(out A t1tg) && t1tg.Value1 == 10 && t1tg.Value2.Item1 == 20f && t1tg.Value2.Item2 == "thirty");

            MyUnion u2 = new B(10f, (20, "thirty"));
            Debug.Assert(u2 is B b && b.Value1 == 10f && b.Value2.Item1 == 20 && b.Value2.Item2 == "thirty");
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B vb && vb.Value1 == 10f && vb.Value2.Item1 == 20 && vb.Value2.Item2 == "thirty");
            Debug.Assert(u2.TryGetValue(out B t2tg) && t2tg.Value1 == 10f && t2tg.Value2.Item1 == 20 && t2tg.Value2.Item2 == "thirty");
            """
            );
    }

    [TestMethod]
    public void TestCaseLayoutOverrides_Overlap()
    {   
        // currently, overlap will be inferred from any type this is a struct without reference type members.
        // and if it is overlappable, the @overlap is perferred.
        // so the @overlap annotation does not actually do anything except cause a diagnostic if the type is deemed not be be overlappable.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            using System;

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(int value);

                // @overlap
                public partial MyUnion(DateOnly value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::System.DateOnly"));
                Assert.IsFalse(HasValueFields(generatedText)); 
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = 10;
            Debug.Assert(u1 is int x && x == 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is int vx && vx == 10);
            Debug.Assert(u1.TryGetValue(out int t1tg) && t1tg == 10);

            MyUnion u2 = DateOnly.Parse("2024-06-06");
            Debug.Assert(u2 is DateOnly d && d == DateOnly.Parse("2024-06-06"));
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is DateOnly vd && vd == DateOnly.Parse("2024-06-06"));
            Debug.Assert(u2.TryGetValue(out DateOnly t2tg) && t2tg == DateOnly.Parse("2024-06-06"));
            """
            );

        // prove that using the @overlap annotation on a non-overlappable type triggers the expected diagnostic
        TestGenerator<CustomUnionSourceGenerator>(
            """
            using System;

            public partial struct MyUnion
            {
                partial void Cases(
                    int case1, 
                    // @overlap
                    <<string case2>>
                    );
            }
            """,
            ["UT0002"]
            );
    }  

    [TestMethod]
    public void TestCaseLayoutOverrides_Box()
    {
        // override normal overlap/decompose/isolate behavior to force a case to be store in an a shared object field.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            using System;

            // @union
            public partial struct MyUnion
            {
                // @box
                public partial MyUnion(int value);

                // @box
                public partial MyUnion((int, float) value);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = 10;
            Debug.Assert(u1 is 10);
            Debug.Assert(u1.Value is int vx && vx == 10);

            MyUnion u2 = (10, 20f);
            Debug.Assert(u2 is ValueTuple<int, float> t2 && t2.Item1 == 10 && t2.Item2 == 20f);
            Debug.Assert(u2.Value is ValueTuple<int, float> v2 && v2.Item1 == 10 && v2.Item2 == 20f);
            """
            );
    }

    [TestMethod]
    public void TestCaseLayoutOverrides_Isolate()
    {
        // override normal overlap/decompose behavior and force to isolate this case
        TestGenerator<CustomUnionSourceGenerator>(
            """
            using System;

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(int value);

                // @isolate
                public partial MyUnion(float value);

                // @isolate  
                public partial MyUnion(string value);  // but will store in object field since it is a reference type
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "int", "float", "object?"));  // 
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = 10;
            Debug.Assert(u1 is 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is int vx && vx == 10);
            Debug.Assert(u1.TryGetValue(out int t1tg) && t1tg == 10);

            MyUnion u2 = 20f;
            Debug.Assert(u2 is 20f);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is float vf && vf == 20f);
            Debug.Assert(u2.TryGetValue(out float t2tg) && t2tg == 20f);
            
            MyUnion u3 = "thirty";
            Debug.Assert(u3 is "thirty");
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is string vs && vs == "thirty");
            Debug.Assert(u3.TryGetValue(out string t3tg) && t3tg == "thirty");
            """
            );
    }

    [TestMethod]
    public void TestCaseLayoutOverrides_Decompose()
    {
        // override normal overlap behavior and force to decompose this case
        TestGenerator<CustomUnionSourceGenerator>(
            """
            using System;

            public record struct A(int X, float Y);
            public record struct B { public int X { get; init; } public float Y { get; init; } }

            // @union
            public partial struct MyUnion
            {
                // @decompose
                public partial MyUnion(A case1);

                // @decompose
                public partial MyUnion(B case2);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "(int, float)"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(int, float)"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A(10, 20f);
            Debug.Assert(u1 is A(10, 20f));
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A v1 && v1.X == 10 && v1.Y == 20f);
            Debug.Assert(u1.TryGetValue(out A t1tg) && t1tg.X == 10 && t1tg.Y == 20f);

            MyUnion u2 = new B { X = 10, Y = 20f };
            Debug.Assert(u2 is B { X: 10, Y: 20f });
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B v2 && v2.X == 10 && v2.Y == 20f);
            Debug.Assert(u2.TryGetValue(out B t2tg) && t2tg.X == 10 && t2tg.Y == 20f);
            """
            );
    }

    [TestMethod]
    public void TestNonDisjointCases_Interfaces()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public interface IA { }
            public interface IB { }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(IA case1);
                public partial MyUnion(IB case2);
                public partial MyUnion(int case3);   // case added to keep generator from switching to boxed layout
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?", "int"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::IB"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::IA"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A();
            Debug.Assert(u1 is IA);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is IA);
            Debug.Assert(u1.TryGetValue(out IA _));

            MyUnion u2 = new B();
            Debug.Assert(u2 is IB);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is IB);
            Debug.Assert(u2.TryGetValue(out IB _));

            MyUnion u3 = new C();
            Debug.Assert(u3 is IA && u3 is IB);
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is IA && u3.Value is IB);
            Debug.Assert(u3.TryGetValue(out IA _) && u3.TryGetValue(out IB _));

            public class A : IA {}
            public class B : IB {}
            public class C : IA, IB {}
            """
        );
    }

    [TestMethod]
    public void TestNonDisjointCases_InterfacesAndImplementingStruct()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public interface IA { }
            public struct B : IA { }
            public struct C { }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(IA value);
                public partial MyUnion(B value);
                public partial MyUnion(C value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::B"));    // struct B implements IA, so case 2 can be retrieved from case 1
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::IA"));   // struct B implements IA, so case 1 can be retrieved from case 2
                Assert.IsFalse(HasTryGetWhen(generatedText, 3, "global::C"));   // struct C does not implement IA
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A();
            Debug.Assert(u1 is IA);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is IA);
            Debug.Assert(u1.TryGetValue(out IA _));

            MyUnion u2 = new B();
            Debug.Assert(u2 is IA && u2 is B);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is IA && u2.Value is B);
            Debug.Assert(u2.TryGetValue(out IA _) && u2.TryGetValue(out B _));

            MyUnion u3 = new B();
            Debug.Assert(u3 is IA && u3 is B);
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is IA && u3.Value is B);
            Debug.Assert(u3.TryGetValue(out IA _) && u3.TryGetValue(out B _));

            MyUnion u4 = new C();
            Debug.Assert(u4 is C);
            Debug.Assert(u4.HasValue);
            Debug.Assert(u4.Value is C);
            Debug.Assert(u4.TryGetValue(out C _));

            public class A : IA {}
            """
        );
    }

    [TestMethod]
    public void TestNonDisjointCases_InterfacesAndClasses()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public interface IA { }

            public struct B  { }         // known to not implement IA
            public class C { }           // not known to implement IA, but unsealed
            public sealed class D { }    // known to not implement IA

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(IA value);
                public partial MyUnion(B value);
                public partial MyUnion(C value);
                public partial MyUnion(D value);
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
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A();
            Debug.Assert(u1 is IA);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is IA);
            Debug.Assert(u1.TryGetValue(out IA _));

            MyUnion u2 = new B();
            Debug.Assert(u2 is B);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B);
            Debug.Assert(u2.TryGetValue(out B _));

            MyUnion u3 = new C();
            Debug.Assert(u3 is C && u3 is not IA);
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is C && u3.Value is not IA);
            Debug.Assert(u3.TryGetValue(out C _) && !u3.TryGetValue(out IA _));

            MyUnion u4 = new CA();
            Debug.Assert(u4 is C && u4 is IA);
            Debug.Assert(u4.HasValue);
            Debug.Assert(u4.Value is C && u4.Value is IA);
            Debug.Assert(u4.TryGetValue(out C _) && u4.TryGetValue(out IA _));
            
            MyUnion u5 = new D();
            Debug.Assert(u5 is D);
            Debug.Assert(u5.HasValue);
            Debug.Assert(u5.Value is D);
            Debug.Assert(u5.TryGetValue(out D _));

            public class A : IA {}
            public class CA : C, IA {}
            """
        );
    }

    [TestMethod]
    public void TestNonDisjointCases_AnythingAndTypeParameter()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion<T>
            {
                public partial MyUnion(int case1);
                public partial MyUnion(T case2);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "int", "T"));

                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "T"));
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "int"));
            },
            """
            using System;
            using System.Diagnostics;

            Test<float>(20f);
            Test<string>("hello");

            void Test<T>(T tval)
                where T : IEquatable<T>
            {
                MyUnion<T> u1 = 10;
                Debug.Assert(u1 is 10);
                Debug.Assert(u1.HasValue);
                Debug.Assert(u1.Value is 10);
                Debug.Assert(u1.TryGetValue(out int ov1) && ov1 == 10);

                MyUnion<T> u2 = tval;
                Debug.Assert(u2 is T tv2 && tv2.Equals(tval));
                Debug.Assert(u2.HasValue);
                Debug.Assert(u2.Value is T && ((T)u2.Value).Equals(tval));
                Debug.Assert(u2.TryGetValue(out T otv2) && otv2.Equals(tval));
            }
            """
        );
    }

    [TestMethod]
    public void TestNonDisjointCases_Subclasses()
    {
        // subclasses are not disjoint because a value of a sub type can be stored as a case of the base type.

        TestGenerator<CustomUnionSourceGenerator>(
            """
            public class A { }
            public class B : A { }
            public class C : B { }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion(B value);
                public partial MyUnion(C value);
                public partial MyUnion(int value); // case added to keep generator from switching to boxed layout
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));  // no overlappable case types types or decomposable cases with overlappable members
                Assert.IsTrue(HasValueFields(generatedText, "object?", "int"));

                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::B"));    // B is a subclass of A
                Assert.IsTrue(HasTryGetWhen(generatedText, 1, "global::C"));    // C is a subclass of A
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::A"));    // A is a superclass of B
                Assert.IsTrue(HasTryGetWhen(generatedText, 2, "global::C"));    // C is a subclass of B
                Assert.IsTrue(HasTryGetWhen(generatedText, 3, "global::A"));    // A is a superclass of C
                Assert.IsTrue(HasTryGetWhen(generatedText, 3, "global::B"));    // B is a superclass of C
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A();
            Debug.Assert(u1 is A);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A);
            Debug.Assert(u1.TryGetValue(out A ov1));

            MyUnion u2 = new B();
            Debug.Assert(u2 is B && u2 is A);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B && u2.Value is A);
            Debug.Assert(u2.TryGetValue(out B ov2b) && u2.TryGetValue(out A ov2a));

            MyUnion u3 = new C();
            Debug.Assert(u3 is C && u3 is B && u3 is A);
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is C && u3.Value is B && u3.Value is A);
            Debug.Assert(u3.TryGetValue(out C ov3c) && u3.TryGetValue(out B ov3b) && u3.TryGetValue(out A ov3a));
            """
            );
    }

    [TestMethod]
    public void TestGenericCases()
    {
        // subclasses are not disjoint because a value of a sub type can be stored as a case of the base type.

        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion<T1, T2>
            {
                public partial MyUnion(T1 value);
                public partial MyUnion(T2 value);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "T1", "T2"));
            },
            """
            using System;
            using System.Diagnostics;

            Test(1, "one");
            Test("one", 1);

            void Test<T1, T2>(T1 t1Val, T2 t2Val)
            {
                MyUnion<T1, T2> u1 = t1Val;
                Debug.Assert(u1 is T1);
                Debug.Assert(u1.HasValue);
                Debug.Assert(u1.Value is T1);
                Debug.Assert(u1.TryGetValue(out T1 ov1));

                MyUnion<T1, T2> u2 = t2Val;
                Debug.Assert(u2 is T2);
                Debug.Assert(u2.HasValue);
                Debug.Assert(u2.Value is T2);
                Debug.Assert(u2.TryGetValue(out T2 ov2));
            }
            """
            );
    }

    [TestMethod]
    public void TestInNamespace()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            namespace MyNamespace
            {
                // @union
                public partial struct MyUnion
                {
                    public partial MyUnion(int case1);
                    public partial MyUnion(string case2);
                }
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(HasNamespaceDeclaration(generatedText, "MyNamespace"));
                Assert.IsTrue(HasValueFields(generatedText, "int", "object?"));
            },
            """
            using System;
            using System.Diagnostics;

            MyNamespace.MyUnion u = 10;
            Debug.Assert(u is 10);
            Debug.Assert(u.HasValue);
            Debug.Assert(u.Value is 10);
            Debug.Assert(u.TryGetValue(out int ov1) && ov1 == 10);

            MyNamespace.MyUnion u2 = "ten";
            Debug.Assert(u2 is "ten");
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is "ten");
            Debug.Assert(u2.TryGetValue(out string ov2) && ov2 == "ten");
            """
            );
    }

    [TestMethod]
    public void TestUsings()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            global using System.Runtime;
            using System.Collections.Generic;
            using X=System.Collections.Generic.List<int>;
    
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(int value);
                public partial MyUnion(string value);
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
        TestGenerator<CustomUnionSourceGenerator>(
            """
            namespace OtherNamespace
            {
                public struct A { public int X; }
                public struct B { public string Y; }
            }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(OtherNamespace.A value);
                public partial MyUnion(OtherNamespace.B value);
            }
            """,
            generatedText =>
            {
                // will be decomposed because it is a struct with public fields (no shenanigans)
                Assert.IsTrue(HasValueFields(generatedText, "int", "object?"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new OtherNamespace.A { X = 1 };
            Debug.Assert(u1 is OtherNamespace.A);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is OtherNamespace.A);
            Debug.Assert(u1.TryGetValue(out OtherNamespace.A ov1));

            MyUnion u2 = new OtherNamespace.B { Y = "one" };
            Debug.Assert(u2 is OtherNamespace.B);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is OtherNamespace.B);
            Debug.Assert(u2.TryGetValue(out OtherNamespace.B ov2) && ov2.Y == "one");
            """
            );
    }

    [TestMethod]
    public void TestCaseTypesInsideOtherTypes()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public static class OtherType
            {
                public struct A { public int X; }
                public struct B { public string Y; }
            }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(OtherType.A value);
                public partial MyUnion(OtherType.B value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasValueFields(generatedText, "int", "object?"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new OtherType.A { X = 1 };
            Debug.Assert(u1 is OtherType.A);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is OtherType.A);
            Debug.Assert(u1.TryGetValue(out OtherType.A ov1));

            MyUnion u2 = new OtherType.B { Y = "one" };
            Debug.Assert(u2 is OtherType.B);
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is OtherType.B);
            Debug.Assert(u2.TryGetValue(out OtherType.B ov2) && ov2.Y == "one");
            """
            );
    }

    [TestMethod]
    public void TestInternalUnion_PublicCases()
    {
        // explicit accessibilty
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            internal partial struct MyUnion
            {
                public partial MyUnion(int value);
                public partial MyUnion(string value);
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(generatedText.Contains("internal partial struct MyUnion"));
            });

        // unspecifed accessibility, should default to internal
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            partial struct MyUnion
            {
                public partial MyUnion(int value);
                public partial MyUnion(string value);
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(generatedText.Contains("internal partial struct MyUnion"));
            });
    }

    [TestMethod]
    public void TestInternalUnion_InternalCases()
    {
        // internal union with public constructors
        TestGenerator<CustomUnionSourceGenerator>(
            """
            internal struct A { }
            internal struct B { }

            // @union
            internal partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion(B value);
            }
            """,
            generatedText => 
            {
                Assert.IsTrue(generatedText.Contains("internal partial struct MyUnion"));
                Assert.IsTrue(generatedText.Contains("internal bool TryGetValue([NotNullWhen(true)] out global::A value)"));
                Assert.IsTrue(generatedText.Contains("internal bool TryGetValue([NotNullWhen(true)] out global::B value)"));
            });

        // internal union with internal constructors
        TestGenerator<CustomUnionSourceGenerator>(
            """
            internal struct A { }
            internal struct B { }

            // @union
            internal partial struct MyUnion
            {
                internal partial MyUnion(A value);
                internal partial MyUnion(B value);
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
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(int? value);
                public partial MyUnion(float? value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = 10;
            Debug.Assert(u1 is 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is 10);
            Debug.Assert(u1.TryGetValue(out int? ov1) && ov1 == 10);

            MyUnion u2 = (int?)null;
            Debug.Assert(u2 is null);
            Debug.Assert(!u2.HasValue);
            Debug.Assert(u2.Value is null);
            Debug.Assert(!u2.TryGetValue(out int? ov2));

            MyUnion u3 = 20f;
            Debug.Assert(u3 is 20f);
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is 20f);
            Debug.Assert(u3.TryGetValue(out float? ov3) && ov3 == 20f);

            MyUnion u4 = (float?)null;
            Debug.Assert(u4 is null);
            Debug.Assert(!u4.HasValue);
            Debug.Assert(u4.Value is null);
            Debug.Assert(!u4.TryGetValue(out float? ov4));
            """
            );
    }

    [TestMethod]
    public void TestNullableCases_ExplicitNullableTypes()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(System.Nullable<int> value);
                public partial MyUnion(System.Nullable<float> value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "float"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = 10;
            Debug.Assert(u1 is 10);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is 10);
            Debug.Assert(u1.TryGetValue(out int? ov1) && ov1 == 10);

            MyUnion u2 = (int?)null;
            Debug.Assert(u2 is null);
            Debug.Assert(!u2.HasValue);
            Debug.Assert(u2.Value is null);
            Debug.Assert(!u2.TryGetValue(out int? ov2));

            MyUnion u3 = 20f;
            Debug.Assert(u3 is 20f);
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is 20f);
            Debug.Assert(u3.TryGetValue(out float? ov3) && ov3 == 20f);

            MyUnion u4 = (float?)null;
            Debug.Assert(u4 is null);
            Debug.Assert(!u4.HasValue);
            Debug.Assert(u4.Value is null);
            Debug.Assert(!u4.TryGetValue(out float? ov4));
            """
            );
    }

    [TestMethod]
    public void TestNullableCases_ReferenceTypes()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public class A { }
            public class B { }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A? value);
                public partial MyUnion(B? value);
            }
            """,
            generatedText =>
            {
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A();
            Debug.Assert(u1 is A);
            Debug.Assert(u1.Value is A);

            MyUnion u2 = (A?)null;
            Debug.Assert(u2 is null);
            Debug.Assert(u2.Value is null);

            MyUnion u3 = new B();
            Debug.Assert(u3 is B);
            Debug.Assert(u3.Value is B);

            MyUnion u4 = (B?)null;
            Debug.Assert(u4 is null);
            Debug.Assert(u4.Value is null);
            """
            );
    }

    [TestMethod]
    public void TestNullableCases_OverlappableStructs()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public record struct A (int X, float Y);
            public record struct B { public required int X { get; init; } public required float Y { get; init; } }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A? value);
                public partial MyUnion(B? value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "global::A"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "global::B"));
                Assert.IsFalse(HasValueFields(generatedText));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A(10, 20f);
            Debug.Assert(u1 is A(var x, var y) && x == 10 && y == 20f);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A a1 && a1.X == 10 && a1.Y == 20f);
            Debug.Assert(u1.TryGetValue(out A? ov1) && ov1 is A(var ox, var oy) && ox == 10 && oy == 20f);

            MyUnion u2 = (A?)null;
            Debug.Assert(u2 is null);
            Debug.Assert(!u2.HasValue);
            Debug.Assert(u2.Value is null);
            Debug.Assert(!u2.TryGetValue(out A? _));

            MyUnion u3 = new B { X = 10, Y = 20f };
            Debug.Assert(u3 is B { X: 10, Y: 20f });
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is B { X: 10, Y: 20f });
            Debug.Assert(u3.TryGetValue(out B? ov3) && ov3 is B { X: 10, Y: 20f });

            MyUnion u4 = (B?)null;
            Debug.Assert(u4 is null);
            Debug.Assert(!u4.HasValue);
            Debug.Assert(u4.Value is null);
            Debug.Assert(!u4.TryGetValue(out B? _));
            """
        );
    }

    [TestMethod]
    public void TestNullableCases_DecomposableStructs()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public record struct A (int X, string Y);
            public record struct B { public required int X { get; init; } public required string Y { get; init; } }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A? value);
                public partial MyUnion(B? value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "int"));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A(10, "ten");
            Debug.Assert(u1 is A(var x, var y) && x == 10 && y == "ten");
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A a1 && a1.X == 10 && a1.Y == "ten");
            Debug.Assert(u1.TryGetValue(out A? ov1) && ov1 is A(var ox, var oy) && ox == 10 && oy == "ten");

            MyUnion u2 = (A?)null;
            Debug.Assert(u2 is null);
            Debug.Assert(!u2.HasValue);
            Debug.Assert(u2.Value is null);
            Debug.Assert(!u2.TryGetValue(out A? _));

            MyUnion u3 = new B { X = 10, Y = "ten" };
            Debug.Assert(u3 is B { X: 10, Y: "ten" });
            Debug.Assert(u3.HasValue);
            Debug.Assert(u3.Value is B { X: 10, Y: "ten" });
            Debug.Assert(u3.TryGetValue(out B? ov3) && ov3 is B { X: 10, Y: "ten" });

            MyUnion u4 = (B?)null;
            Debug.Assert(u4 is null);
            Debug.Assert(!u4.HasValue);
            Debug.Assert(u4.Value is null);
            Debug.Assert(!u4.TryGetValue(out B? _));
            """
        );
    }

    [TestMethod]
    public void TestNullableCaseMembers_DecomposableStructs()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            public record struct A (int? X, string? Y);
            public record struct B { public int? X { get; init; } public string? Y { get; init; } public float? Z { get; init; } }

            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(A value);
                public partial MyUnion(B value);
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasOverlappedField(generatedText));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 1, "int?"));
                Assert.IsTrue(HasOverlappedCaseField(generatedText, 2, "(int?, float?)"));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = new A(10, null);
            Debug.Assert(u1 is A(var x, var y) && x == 10 && y is null);
            Debug.Assert(u1.HasValue);
            Debug.Assert(u1.Value is A a1 && a1.X == 10 && a1.Y is null);
            Debug.Assert(u1.TryGetValue(out A ov1) && ov1 is A(var ox, var oy) && ox == 10 && oy is null);

            MyUnion u2 = new B { X = 10, Y = "ten", Z = null };
            Debug.Assert(u2 is B { X: 10, Y: "ten", Z: null });
            Debug.Assert(u2.HasValue);
            Debug.Assert(u2.Value is B { X: 10, Y: "ten", Z: null });
            Debug.Assert(u2.TryGetValue(out B ov2) && ov2 is B { X: 10, Y: "ten", Z: null });
            """
        );
    }

    [TestMethod]
    public void TestDiagnostics_UnsupportedCaseTypes()
    {
        // prove that unions with cases or case members that have unsupported types (e.g. pointers, spans, etc.) produce diagnostics.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                <<public partial MyUnion(Span<int> value);>>
                <<public partial MyUnion(ReadOnlySpan<float> value);>>
                <<public partial MyUnion(int* value);>>
                <<public partial MyUnion(A value);>>
                <<public partial MyUnion(B value);>>
            }

            public ref struct A { public int X; }
            public struct B { public int* X; }
            """,
            ["UT0001", "UT0001", "UT0001", "UT0001", "UT0001"]
            );
    }

    [TestMethod]
    public void TestDiagnostics_NonoverlappableCaseTypes()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(int value);

                // @overlap
                <<public partial MyUnion(string value);>>

                // @overlap
                <<public partial MyUnion(AB value);>>
            }

            struct AB { public int X; public string Y; }
            """,
            ["UT0002", "UT0002"]
            );
    }

    [TestMethod]
    public void TestDiagnostics_NondecomposableCaseTypes()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                // @decompose
                <<public partial MyUnion(int value);>>  // cannot decompose int

                // @decompose
                <<public partial MyUnion(string value);>>   // cannot decompose string

                // @decompose
                public partial MyUnion(A value);        // okay, if you say so, but z is lost

                // @decompose
                public partial MyUnion(B value);        // okay, was going to anyway
            }

            public struct A { public int X; public string Y; internal int z; }
            public struct B { public int X { get; set; } public string Y { get; init; } }
            """,
            ["UT0003", "UT0003"]
            );
    }

    [TestMethod]
    public void TestBoxLayout_InferredBoxedCases()
    {
        // if all cases prefer box layout, then the union will be boxed layout.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(string value);
                public partial MyUnion(A value);
                public partial MyUnion(B value);
            }

            public record A(int X);
            public record B(int X);
            """,
            generatedText =>
            {
                Assert.IsFalse(HasTagField(generatedText));
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            }
            );
    }

    [TestMethod]
    public void TestBoxLayout_OverrideBoxedCases()
    {
        // if all cases prefer box layout, then the union will be boxed layout.
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(string case1);

                // @box
                public partial MyUnion(int case2);
                
                // @box
                public partial MyUnion(A case3);
            }

            public record struct A(int X);
            """,
            generatedText =>
            {
                Assert.IsFalse(HasTagField(generatedText));
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?"));
            }
            );
    }

    [TestMethod]
    public void TestDefaultProperty()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(string value);
                public partial MyUnion(int value);

                public static MyUnion Default { get; } = "default";
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasTagField(generatedText));
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?", "int"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = default;
            Debug.Assert(u1 is "default");
            """
            );
    }

    [TestMethod]
    public void TestDefaultProperty_NullDefault()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(string? value);
                public partial MyUnion(int value);

                public static MyUnion Default { get; } = (string?)null;
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasTagField(generatedText));
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?", "int"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = default;
            Debug.Assert(u1 is null);
            """
            );
    }

    [TestMethod]
    public void TestDefaultProperty_NullCase()
    {
        TestGenerator<CustomUnionSourceGenerator>(
            """
            // @union
            public partial struct MyUnion
            {
                public partial MyUnion(string? value);
                public partial MyUnion(int value);

                public static MyUnion Default { get; } = "default";
            }
            """,
            generatedText =>
            {
                Assert.IsTrue(HasTagField(generatedText));
                Assert.IsFalse(HasOverlappedField(generatedText));
                Assert.IsTrue(HasValueFields(generatedText, "object?", "int"));
            },
            """
            using System;
            using System.Diagnostics;

            MyUnion u1 = default;
            Debug.Assert(u1 is "default");

            MyUnion u2 = (string?)null;
            Debug.Assert(u2 is null);
            """
            );
    }

    #region Generated Text Checks

    /// <summary>
    /// Returns true if the generated text contains a declaration for the tag field.
    /// </summary>
    private static bool HasTagField(string text) =>
        text.Contains("_kind");

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
        if (fieldTypes.Length == 1 
            && fieldTypes[0] == "object?")
        {
            // if only one value field is expected and it is an object, then it can be named either _value or _value1
            if (!text.Contains("object? _value;") && !text.Contains("object? _value1;"))
                return false;
        }
        else
        {
            // check for existence of each expected value field in order.
            for (int i = 0; i < fieldTypes.Length; i++)
            {
                var fieldType = fieldTypes[i];
                var test = $"{fieldType} _value{i + 1};";
                if (!text.Contains(test))
                    return false;
            }           
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

    #endregion

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
