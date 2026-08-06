using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using UnionTypes.Toolkit;
using UnionTypes.Toolkit.Generators;

namespace Tests;

[TestClass]
public class CustomUnionTests
{
    [TestMethod]
    public void TestSimplePrimitivesUnion()
    {
        TestGenerator(
            """
            // @OverlappedUnion
            public partial struct MyUnion
            {
                partial void Cases(int case1, float case2);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion u = 10;
                    MyUnion u2 = 5.0f;
                    if (u is 10) { }
                    if (u is 5.0f) { }
                }
            }
            """,
            generatedText =>
            {
                // prove that the two primitives got overlapped into the overlapped field
                return generatedText.Contains("_overlapped");
            });
    }

    [TestMethod]
    public void TestDeconstructableCasesWithOverlappableMembers()
    {
        TestGenerator(
            """
            // @OverlappedUnion
            public partial struct MyUnion
            {
                partial void Cases(A case1, B case2);
            }

            public record struct A(int Value);
            public record struct B(float Value);

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion u = new A(10);
                    MyUnion u2 = new B(5.0f);
                    if (u is A a) { }
                    if (u is B b) { }
                }
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                return generatedText.Contains("_overlapped")
                    && generatedText.Contains("int Case1")          // part of overlapped struct
                    && generatedText.Contains("float Case2");       // part of overlapped struct
            });
    }

    [TestMethod]
    public void TestDeconstructableCasesWithMultipleOverlappableMembers()
    {
        TestGenerator(
            """
            // @OverlappedUnion
            public partial struct MyUnion
            {
                partial void Cases(A case1, B case2);
            }

            public record struct A(int Value1, float Value2);
            public record struct B(float Value);

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion u = new A(10, 3.0f);
                    MyUnion u2 = new B(5.0f);
                    if (u is A a) { }
                    if (u is B b) { }
                }
            }
            """,
            generatedText =>
            {
                // prove that contents of the two decomposable cases got overlapped into the overlapped field
                return generatedText.Contains("_overlapped")
                    && generatedText.Contains("(int Value1, float Value2) Case1")          // part of overlapped struct
                    && generatedText.Contains("float Case2");       // part of overlapped struct
            });
    }


#if false
    [TestMethod]
    public void TestTypeUnion_NestedRecords()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                [Case]
                public record struct A(int x);

                [Case]
                public record struct B(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new MyUnion.A(10));
                    MyUnion unionB = MyUnion.Create(new MyUnion.B("x", 5.0f));
                    MyUnion.A a = unionA.AValue;
                    MyUnion.B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_NestedRecords_CaseNames()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                [Case(Name="Aa")]
                public record struct A(int x);

                [Case(Name="Bb")]
                public record struct B(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new MyUnion.A(10));
                    MyUnion unionB = MyUnion.Create(new MyUnion.B("x", 5.0f));
                    MyUnion.A a = unionA.AaValue;
                    MyUnion.B b = unionB.BbValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.Aa;
                    var correctTagB = unionB.Kind == MyUnion.Case.Bb; 
                }
            }
            """,
            newText =>
            {
                return newText.Contains("Aa = 1")
                    && newText.Contains("Bb = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_NestedRecords_FactoryNames()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                [Case(FactoryName="MakeA")]
                public record struct A(int x);

                [Case(FactoryName="MakeB")]
                public record struct B(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.MakeA(new MyUnion.A(10));
                    MyUnion unionB = MyUnion.MakeB(new MyUnion.B("x", 5.0f));
                    MyUnion.A a = unionA.AValue;
                    MyUnion.B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B; 
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_NestedRecords_TagValues()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                [Case(TagValue=4)]
                public record struct A(int x);

                [Case(TagValue=3)]
                public record struct B(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new MyUnion.A(10));
                    MyUnion unionB = MyUnion.Create(new MyUnion.B("x", 5.0f));
                    MyUnion.A a = unionA.AValue;
                    MyUnion.B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 4")
                    && newText.Contains("B = 3");
            });
    }

    [TestMethod]
    public void TestTypeUnion_FactoryMethods()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct A(int x);
            public record struct B(string y, float z);

            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(A a);
                public static partial MyUnion Create(B b);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new A(10));
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    A a = unionA.AValue;
                    B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_FactoryMethods_CaseNames()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct A(int x);
            public record struct B(string y, float z);

            [TypeUnion]
            public partial struct MyUnion
            {
                [Case(Name="Aa")]
                public static partial MyUnion Create(A a);

                [Case(Name="Bb")]
                public static partial MyUnion Create(B b);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new A(10));
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    A a = unionA.AaValue;
                    B b = unionB.BbValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.Aa;
                    var correctTagB = unionB.Kind == MyUnion.Case.Bb;
                }
            }
            """,
            newText =>
            {
                // tag values
                return newText.Contains("Aa = 1")
                    && newText.Contains("Bb = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_FactoryMethods_TagValues()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct A(int x);
            public record struct B(string y, float z);

            [TypeUnion]
            public partial struct MyUnion
            {
                [Case(TagValue=4)]
                public static partial MyUnion Create(A a);

                [Case(TagValue=3)]
                public static partial MyUnion Create(B b);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new A(10));
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    A a = unionA.AValue;
                    B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 4")
                    && newText.Contains("B = 3");
            });
    }

    [TestMethod]
    public void TestTypeUnion_CasesOnType()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct A(int x);
            public record struct B(string y, float z);

            [TypeUnion]
            [Case(Type=typeof(A))]
            [Case(Type=typeof(B))]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new A(10));
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    A a = unionA.AValue;
                    B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;                
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_CasesOnType_CaseNames()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct A(int x);
            public record struct B(string y, float z);

            [TypeUnion]
            [Case(Name="Aa", Type=typeof(A))]
            [Case(Name="Bb", Type=typeof(B))]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new A(10));
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    A a = unionA.AaValue;
                    B b = unionB.BbValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.Aa;
                    var correctTagB = unionB.Kind == MyUnion.Case.Bb;                
                }
            }
            """,
            newText =>
            {
                return newText.Contains("Aa = 1")
                    && newText.Contains("Bb = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_CasesOnType_FactoryNames()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct A(int x);
            public record struct B(string y, float z);

            [TypeUnion]
            [Case(Type=typeof(A), FactoryName="MakeA")]
            [Case(Type=typeof(B), FactoryName="MakeB")]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.MakeA(new A(10));
                    MyUnion unionB = MyUnion.MakeB(new B("x", 5.0f));
                    A a = unionA.AValue;
                    B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;                
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_CasesOnType_TagValues()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct A(int x);
            public record struct B(string y, float z);

            [TypeUnion]
            [Case(Type=typeof(A), TagValue=4)]
            [Case(Type=typeof(B), TagValue=3)]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(new A(10));
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    A a = unionA.AValue;
                    B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;                
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 4")
                    && newText.Contains("B = 3");
            });
    }

    [TestMethod]
    public void TestTypeUnion_CasesOnType_Singleton()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public class A { private A(){} public static readonly A Value = new A(); }
            public record struct B(string y, float z);

            [TypeUnion]
            [Case(Type=typeof(A))]
            [Case(Type=typeof(B))]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.Create(A.Value);
                    MyUnion unionA2 = A.Value;
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    MyUnion unionB2 = new B("x", 5.0f);
                    A a = unionA.AValue;
                    B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;                
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_CasesOnType_Singleton_FactoryKind_Property()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public class A { private A() {} public static readonly A Singleton = new A(); }
            public record struct B(string y, float z);

            [TypeUnion]
            [Case(Type=typeof(A), FactoryKind=FactoryKind.Property)]
            [Case(Type=typeof(B))]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.A;
                    MyUnion unionA2 = A.Singleton;
                    MyUnion unionB = MyUnion.Create(new B("x", 5.0f));
                    MyUnion unionB2 = new B("x", 5.0f);
                    A a = unionA.AValue;
                    B b = unionB.BValue;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;                
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.A(10);
                    MyUnion unionB = MyUnion.B("x", 5.0f);
                    var a = unionA.AValue;
                    var b = unionB.BValues;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_Span()
    {
        TestUnion(
            """
            using System;
            using UnionTypes.Toolkit;

            [TagUnion]
            public ref partial struct MyUnion
            {
                public static partial MyUnion A(Span<byte> bytes);
                public static partial MyUnion B(Span<char> characters);
            }

            public class Test
            {
                public void TestMethod()
                {
                }
            }
            """,
            newText => newText.Contains("Span<char> BValue")
                && newText.Contains("Match(")
            );
    }

    [TestMethod]
    public void TestTagUnion_Span_Shared()
    {
        TestUnion(
            """
            using System;
            using UnionTypes.Toolkit;

            [TagUnion]
            public ref partial struct MyUnion
            {
                public static partial MyUnion A(Span<byte> bytes);
                public static partial MyUnion B(Span<byte> bytes, string flavor);
            }

            public class Test
            {
                public void TestMethod()
                {
                }
            }
            """,
            newText => newText.Contains("BTuple BValues")
                && newText.Contains("Match(")
            );
    }

    [TestMethod]
    public void TestTypeUnion_Span()
    {
        TestUnion(
            """
            using System;
            using UnionTypes.Toolkit;

            [TypeUnion]
            public readonly ref partial struct MyUnion
            {
                public static partial MyUnion Create(Span<byte> bytes);
                public static partial MyUnion Create(Span<char> chars);
            }

            public class Test
            {
                public void TestMethod()
                {
                }
            }
            """,
            newText => newText.Contains("Span<byte> BytesValue")
                && newText.Contains("Match(")
            );
    }

    [TestMethod]
    public void TestTagUnion_CasesOnType()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            [Case(Name="A")]
            [Case(Name="B")]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.A;
                    MyUnion unionB = MyUnion.B;
                    var a = unionA.IsA;
                    var b = unionB.IsB;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_CasesOnType_FactoryName()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            [Case(Name="A", FactoryName="MakeA")]
            [Case(Name="B", FactoryName="MakeB")]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.MakeA;
                    MyUnion unionB = MyUnion.MakeB;
                    var a = unionA.IsA;
                    var b = unionB.IsB;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_CasesOnType_FactoryKind_Method()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            [Case(Name="A", FactoryName="A", FactoryKind=FactoryKind.Method)]
            [Case(Name="B", FactoryName="B", FactoryKind=FactoryKind.Method)]
            public partial struct MyUnion
            {
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.A();
                    MyUnion unionB = MyUnion.B();
                    var a = unionA.IsA;
                    var b = unionB.IsB;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_CaseNames()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                [Case(Name="A")]
                public static partial MyUnion MakeA(int x);

                [Case(Name="B")]
                public static partial MyUnion MakeB(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.MakeA(10);
                    MyUnion unionB = MyUnion.MakeB("x", 5.0f);
                    var a = unionA.AValue;
                    var b = unionB.BValues;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_AccessorNames()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                [Case(AccessorName="StuffForA")]
                public static partial MyUnion A(int x);

                [Case(AccessorName="StuffForB")]
                public static partial MyUnion B(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.A(10);
                    MyUnion unionB = MyUnion.B("x", 5.0f);
                    var a = unionA.StuffForA;
                    var b = unionB.StuffForB;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 1")
                    && newText.Contains("B = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_TagValues()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                [Case(TagValue=4)]
                public static partial MyUnion A(int x);

                [Case(TagValue=3)]
                public static partial MyUnion B(string y, float z);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionA = MyUnion.A(10);
                    MyUnion unionB = MyUnion.B("x", 5.0f);
                    var a = unionA.AValue;
                    var b = unionB.BValues;
                    var correctTagA = unionA.Kind == MyUnion.Case.A;
                    var correctTagB = unionB.Kind == MyUnion.Case.B;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("A = 4")
                    && newText.Contains("B = 3");
            });
    }

    [TestMethod]
    public void TestTagUnion_DefaultState()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                [Case(TagValue=0)]
                public static partial MyUnion Nobody();

                public static partial MyUnion Student(string name, decimal grade);

                public static partial MyUnion Teacher(string name);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionN = MyUnion.Nobody();
                    MyUnion unionT = MyUnion.Teacher("Mr. Bob");
                    MyUnion unionS = MyUnion.Student("Alice", 4.0m);
                    var n = unionN.IsNobody;
                    var t = unionT.TeacherValue;
                    var s = unionS.StudentValues;
                    var correctTagN = unionN.Kind == MyUnion.Case.Nobody;
                    var correctTagT = unionT.Kind == MyUnion.Case.Teacher;
                    var correctTagS = unionS.Kind == MyUnion.Case.Student;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("Nobody = 0")
                    && newText.Contains("Student = 1")
                    && newText.Contains("Teacher = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_Sharing()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                [Case(TagValue=0)]
                public static partial MyUnion Nobody();

                public static partial MyUnion Student(string name, decimal grade);

                public static partial MyUnion Teacher(string name, int students);
            }

            public class Test
            {
                public void TestMethod()
                {
                    MyUnion unionN = MyUnion.Nobody();
                    MyUnion unionT = MyUnion.Teacher("Mr. Bob", 5);
                    MyUnion unionS = MyUnion.Student("Alice", 4.0m);
                    var n = unionN.IsNobody;
                    var t = unionT.TeacherValues;
                    var s = unionS.StudentValues;
                    var correctTagN = unionN.Kind == MyUnion.Case.Nobody;
                    var correctTagT = unionT.Kind == MyUnion.Case.Teacher;
                    var correctTagS = unionS.Kind == MyUnion.Case.Student;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("Nobody = 0")
                    && newText.Contains("Student = 1")
                    && newText.Contains("Teacher = 2");
            });
    }

    [TestMethod]
    public void TestTagUnion_Overlap_RecordStruct()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(A a);
                public static partial MyUnion B(int x);
            }

            public record struct A(int x, int y);
            """,
            newText =>
            {
                return newText.Contains("A A_a");
            });
    }

    [TestMethod]
    public void TestTagUnion_Overlap_Tuple()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A((int x, int y) a);
                public static partial MyUnion B(int x);
            }
            """,
            newText =>
            {
                return newText.Contains("(int x, int y) A_a");
            });
    }

    [TestMethod]
    public void TestTagUnion_Decompose_OverlappableRecordStruct()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(OverlapStructs=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(A a);
                public static partial MyUnion B(int x);
            }

            public record struct A(int x, int y);
            """,
            newText =>
            {
                return newText.Contains("_data_shared1")
                    && newText.Contains("_data_a_y")
                    && GetDataFields(newText).Count == 2;
            });
    }

    [TestMethod]
    public void TestTagUnion_Decompose_NonOverlappableRecordStruct()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(A a);
                public static partial MyUnion B(int x);
            }

            public record struct A(int x, string y);
            """,
            newText =>
            {
                return newText.Contains("_data_shared1")
                    && newText.Contains("_data_a_y")
                    && GetDataFields(newText).Count == 2;
            });
    }

    [TestMethod]
    public void TestTagUnion_Decompose_OverlappableTuple()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(OverlapStructs=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A((int x, int y) a);
                public static partial MyUnion B(int x);
            }
            """,
            newText =>
            {
                return newText.Contains("_data_shared1")
                    && newText.Contains("int _data_a_Item2")
                    && GetDataFields(newText).Count == 2;
            });
    }

    [TestMethod]
    public void TestTagUnion_Decompose_NonOverlappableTuple()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(OverlapStructs=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A((int x, string y) a);
                public static partial MyUnion B(int x);
            }
            """,
            newText =>
            {
                return newText.Contains("_data_shared1")
                    && newText.Contains("string _data_a_Item2")
                    && GetDataFields(newText).Count == 2;
            });
    }


    [TestMethod]
    public void TestTagUnion_NonOverlappableNonDecomposableRecordStruct()
    {
        // record structs with additional fields are not decomposable
        // and record structs with non-overlappable parameters are not overlappable
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(A a);
                public static partial MyUnion B(int x);
            }

            public record struct A(int x, string y) { public int P { get; set; } }
            """,
            newText =>
            {

                return newText.Contains("A _data_a_a")
                    && newText.Contains("int _data_b_x")
                    && GetDataFields(newText).Count == 2;
            });
    }

    [TestMethod]
    public void TestTagUnion_Overlap_NonDecomposableRecordStruct()
    {
        // record structs with additional fields are not decomposable
        // but may still overlap
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(A a);
                public static partial MyUnion B(int x);
            }

            public record struct A(int x, int y) { public int P { get; set; } }
            """,
            newText =>
            {
                return newText.Contains("A A_a;");
            });
    }

    [TestMethod]
    public void TestTagUnion_Option()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            [Case(Name="None", TagValue=0)]
            public partial struct Option<T>
            {
                public static partial Option<T> Some(T value);
            }

            public class Test
            {
                public void TestMethod()
                {
                    Option<string> optS = Option<string>.Some("Hello");
                    Option<string> optN = Option<string>.None;
                    var valS = optS.SomeValue;
                    var valN = optN.IsNone;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("None = 0")
                    && newText.Contains("Some = 1");
            });
    }

    [TestMethod]
    public void TestTagUnion_Result()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct Result<T>
            {
                public static partial Result<T> Success(T value);

                [Case(AccessorName="FailureMessage")]
                public static partial Result<T> Failure(string message);
            }

            public class Test
            {
                public void TestMethod()
                {
                    Result<int> success = Result<int>.Success(10);
                    Result<int> failure = Result<int>.Failure("Oops");
                    var value = success.SuccessValue;
                    var message = failure.FailureMessage;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("Success = 1")
                    && newText.Contains("Failure = 2");
            });
    }

    [TestMethod]
    public void TestTypeUnion_Option()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public class None { private None() {} public static readonly None Singleton = new None(); }

            [TypeUnion]
            [Case(Type=typeof(None), TagValue=0, FactoryName="None", FactoryKind=FactoryKind.Property)]
            public partial struct Option<T>
            {
                public static partial Option<T> Some(T value);
            }

            public class Test
            {
                public void TestMethod()
                {
                    Option<string> optS = Option<string>.Some("Hello");
                    Option<string> optSA = "Hello";
                    Option<string> optN = Option<string>.None;
                    Option<string> optNA = None.Singleton;
                    var valS = optS.SomeValue;
                    var valN = optN.NoneValue;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("None = 0")
                    && newText.Contains("Some = 1");
            });
    }

    [TestMethod]
    public void TestTypeUnion_Result()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct Result<T>
            {
                [Case]
                public static partial Result<T> Success(T value);

                [Case(Name="Failure", AccessorName="FailureMessage")]
                public static partial Result<T> Failure(string message);
            }

            public class Test
            {
                public void TestMethod()
                {
                    Result<int> success = Result<int>.Success(10);
                    Result<int> failure = Result<int>.Failure("Oops");
                    var valS = success.SuccessValue;
                    var valF = failure.FailureMessage;
                }
            }
            """,
            newText =>
            {
                return newText.Contains("Success = 1")
                    && newText.Contains("Failure = 2");
            });
    }


    [TestMethod]
    public void TestUnion_InNamespace()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            namespace MyNamespace
            {
                [TypeUnion]
                public partial struct MyUnion
                {
                    public static partial MyUnion Create(int x);
                    public static partial MyUnion Create(string y);
                }
            }
            """,
            newText => newText.Contains("namespace MyNamespace"));
    }

    [TestMethod]
    public void TestUnion_GlobalUsings()
    {
        TestUnion(
            """
            global using System;
            using UnionTypes.Toolkit;

            namespace MyNamespace
            {
                [TypeUnion]
                public partial struct MyUnion
                {
                    public static partial MyUnion Create(int x);
                    public static partial MyUnion Create(string y);
                }
            }
            """,
            newText => newText.Contains("UnionTypes")
                    && !newText.Contains("global"));
    }

    [TestMethod]
    public void TestUnion_TypesInOtherNamespace()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            namespace OtherNamespace
            {
                public record struct A(int x);
                public record struct B(string y);
            }

            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(OtherNamespace.A a);
                public static partial MyUnion Create(OtherNamespace.B b);
            }
            """);
    }

    [TestMethod]
    public void TestUnion_TypesInsideOtherType()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public static class OtherType
            {
                public record struct A(int x);
                public record struct B(string y);
            }

            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(OtherType.A a);
                public static partial MyUnion Create(OtherType.B b);
            }
            """);
    }

    [TestMethod]
    public void TestTypeUnion_Internal()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            internal partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            newText => newText.Contains("internal"));
    }

    [TestMethod]
    public void TestTypeUnion_InternalValues()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            internal record struct A(int x);
            internal record struct B(string y);
                            
            [TypeUnion]
            [Case(Type=typeof(A))]
            [Case(Type=typeof(B))]
            public partial struct MyUnion
            {
            }
            """,
            newText => newText.Contains("internal"));
    }

    [TestMethod]
    public void TestTypeUnion_InternalNestedTypes()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                [Case]
                internal record struct A(int x);

                [Case]
                internal record struct B(string y);
            }
            """,
            newText => newText.Contains("internal"));
    }

    [TestMethod]
    public void TestTypeUnion_InternalFactories()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            record A(int x);
            record B(string y);

            [TypeUnion]
            public partial struct MyUnion
            {
                internal static partial MyUnion A(A value);
                internal static partial MyUnion B(B value);
            }
            """,
            newText => newText.Contains("internal"));
    }

    [TestMethod]
    public void TestTagUnion_Internal()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            internal partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y);
            }
            """,
            newText => newText.Contains("internal"));
    }

    [TestMethod]
    public void TestTagUnion_InternalFactories()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                internal static partial MyUnion A(int x);
                internal static partial MyUnion B(string y);
            }
            """,
            newText => newText.Contains("internal"));
    }

    [TestMethod]
    public void TestTagUnion_ReferenceConstrainedTypeParameter()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct Result<T> where T : class
            {
                public static partial Result<T> Success(T value);
                public static partial Result<T> Failure(string reason);
            }
            """);
    }

    [TestMethod]
    public void TestTypeUnion_ReferencedConstrainedTypeParameter()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct Result<T> where T : class
            {
                [Case]
                public record struct Success(T value);

                [Case]
                public record struct Failure(string reason);
            }
            """);
    }

    [TestMethod]
    public void TestUnion_NoShareReferenceFields()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(ShareReferenceFields=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(string x);
                public static partial MyUnion B(object y);
            }
            """,
            newText =>
            {
                if (newText.Contains("OverlappedData"))
                    return false;

                var fields = GetDataFields(newText);
                if (fields.Count != 2)
                    return false;

                return true;
            });
    }

    [TestMethod]
    public void TestUnion_NoShareSameTypeFields()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(OverlapStructs=false, ShareSameTypeFields=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(int y);
            }
            """,
            newText =>
            {
                if (newText.Contains("OverlappedData"))
                    return false;

                var fields = GetDataFields(newText);
                if (fields.Count != 2)
                    return false;

                return true;
            });
    }

    [TestMethod]
    public void TestUnion_NoOverlapStructs()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(OverlapStructs=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x, long y);
                public static partial MyUnion B(long x, int y);
            }
            """,
            newText =>
            {
                if (newText.Contains("OverlappedData"))
                    return false;

                var fields = GetDataFields(newText);
                if (fields.Count != 2)
                    return false;

                return true;
            });
    }

    [TestMethod]
    public void TestUnion_NoDecomposeStructs()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            public record struct AData(int x, string y);
            public record struct BData(long x, string y);

            [TagUnion(DecomposeStructs=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(AData data);
                public static partial MyUnion B(BData data);
            }
            """,
            newText => !newText.Contains("OverlappedData")
                    && GetDataFields(newText).Count == 2
            );
    }

    [TestMethod]
    public void TestUnion_TagTypeName()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(TagTypeName="Tag")]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y);
            }
            """,
            newText => newText.Contains("enum Tag")
                && newText.Contains("Tag Kind { get; }")
            );
    }

    [TestMethod]
    public void TestUnion_TagPropertyName()
    {
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(TagPropertyName="Tag")]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y);
            }
            """,
            newText => newText.Contains("enum Case")
                && newText.Contains("Case Tag { get; }")
            );
    }

    [TestMethod]
    public void TestTagUnion_GenerateEquality()
    {
        // default is enabled
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y);
            }
            """,
            newText => newText.Contains("bool Equals(")
            );

        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(GenerateEquality=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y);
            }
            """,
            newText => !newText.Contains("bool Equals(")
            );

    }

    [TestMethod]
    public void TestTypeUnion_GenerateEquality()
    {
        // default is enabled
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            newText => newText.Contains("bool Equals(")
            );

        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion(GenerateEquality=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(string y);
            }
            """,
            newText => !newText.Contains("bool Equals(")
            );

    }

    [TestMethod]
    public void TestTagUnion_GenerateToString()
    {
        // default is enabled
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y);
            }
            """,
            newText => newText.Contains("override string ToString(")
            );

        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(GenerateToString=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y);
            }
            """,
            newText => !newText.Contains("override string ToString(")
            );

    }

    [TestMethod]
    public void TestTypeUnion_GenerateToString()
    {
        // default is enabled
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int value);
                public static partial MyUnion Create(string value);
            }
            """,
            newText => newText.Contains("override string ToString(")
            );

        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion(GenerateToString=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int value);
                public static partial MyUnion Create(string value);
            }
            """,
            newText => !newText.Contains("override string ToString(")
            );
    }

    [TestMethod]
    public void TestTagUnion_GenerateMatch()
    {
        // default is enabled
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y, float z);
            }
            """,
            newText => newText.Contains("void Match(")
                && newText.Contains("TResult Select<TResult>(")
            );

        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TagUnion(GenerateMatch=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(string y, float z);
            }
            """,
            newText => !newText.Contains("void Match(")
                && !newText.Contains("TResult Select<TResult>(")
            );

    }

    [TestMethod]
    public void TestTypeUnion_GenerateMatch()
    {
        // default is enabled
        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int value);
                public static partial MyUnion Create(string value);
            }
            """,
            newText => newText.Contains("void Match(")
                && newText.Contains("TResult Select<TResult>(")
            );

        TestUnion(
            """
            using UnionTypes.Toolkit;

            [TypeUnion(GenerateMatch=false)]
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int value);
                public static partial MyUnion Create(string value);
            }
            """,
            newText => !newText.Contains("void Match(")
                && !newText.Contains("TResult Select<TResult>(")
            );
    }

    [TestMethod]
    public void TestTypeUnion_NoToolkit()
    {
        TestUnion(
            """
            // @TypeUnion
            public partial struct MyUnion
            {
                public static partial MyUnion Create(int x);
                public static partial MyUnion Create(double x);
            }
            """,
            newText => 
                newText.Contains("Int32 = 1")
                && newText.Contains("Double = 2")
            );
    }

    [TestMethod]
    public void TestTagUnion_NoToolkit()
    {
        TestUnion(
            """
            // @TagUnion
            public partial struct MyUnion
            {
                public static partial MyUnion CreateA(int x);
                public static partial MyUnion CreateB(double x);
            }
            """,
            newText => newText.Contains("MyUnion") 
                && newText.Contains("A = 1")
                && newText.Contains("B = 2")
                && !newText.Contains("TryGet")
            );

        TestUnion(
            """
            // @TagUnion
            public partial struct MyUnion
            {
                public static partial MyUnion A(int x);
                public static partial MyUnion B(double x);
            }
            """,
            newText => newText.Contains("MyUnion")
                && newText.Contains("A = 1")
                && newText.Contains("B = 2")
                && !newText.Contains("TryGet")
            );
    }

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
    #endif

    private void TestGenerator(string sourceText, Func<string, bool>? generatedTextAssertion = null)
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

        if (generatedTextAssertion != null)
        {
            Assert.IsTrue(generatedTextAssertion(newText), "generated text assertion failed");
        }
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

    private static void AssertNoDiagnostics(ImmutableArray<Diagnostic> diagnostics, string newText)
    {
        if (diagnostics.Length > 0)
        {
            Assert.Fail($"Unexpected diagnostic: {diagnostics[0]}");
        }
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        return CSharpCompilation.Create(
            assemblyName: "compilation",
            syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions)).ToArray(),
            references: new[] { Core, Netstandard, SystemRuntime },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    private static string RuntimeDirectory = 
        Path.GetDirectoryName(typeof(Enumerable).GetTypeInfo().Assembly.Location)!;
    
    private static readonly MetadataReference Netstandard =
        MetadataReference.CreateFromFile(Path.Combine(RuntimeDirectory, "netstandard.dll"));

    private static readonly MetadataReference SystemRuntime =
        MetadataReference.CreateFromFile(Path.Combine(RuntimeDirectory, "System.Runtime.dll"));

    private static readonly MetadataReference Core =
        MetadataReference.CreateFromFile(typeof(int).GetTypeInfo().Assembly.Location);

    // private static readonly MetadataReference UnionTypes =
    //     MetadataReference.CreateFromFile(typeof(TypeUnionAttribute).GetTypeInfo().Assembly.Location);
}
