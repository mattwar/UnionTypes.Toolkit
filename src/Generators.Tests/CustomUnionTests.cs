using UnionTypes.Toolkit.Generators;

namespace Tests
{
    [TestClass]
    public class CustomUnionTests
    {
        [TestMethod]
        public void Test_SimplePrimitives()
        {
            TestGenerate(
                new UnionInfo(
                    "TagLikeUnion",
                    [
                        new CaseDesc(TypeDesc.Int32),
                        new CaseDesc(TypeDesc.Float),
                    ]
                    )
                );
        }

        [TestMethod]
        public void Test_DeconstructableCases()
        {
            TestGenerate(
                new UnionInfo(
                    "TagLikeUnion",
                    [
                        new CaseDesc(
                            new TypeDesc("Open", [new MemberDesc("Percent", TypeDesc.Float, isParameter: true)]),
                            generateType: true
                            ),  
                        new CaseDesc(
                            new TypeDesc("Closed", [new MemberDesc("Percent", TypeDesc.Float, isParameter: true)]),
                            generateType: true
                            ),
                    ]
                    )
                );
        }

#if false
        [TestMethod]
        public void TestTagUnion_Option()
        {
            TestGenerate(
                new Union(
                    UnionKind.TypeUnion,
                    "Option",
                    "Option<TValue>",
                    "public",
                    new[]
                    {
                        new UnionCase(
                            name: "Some",
                            type: null,
                            tagValue: 1,
                            factoryName: "Some",
                            factoryParameters: new [] { new UnionCaseValue("value", new UnionValueType("TValue", TypeKind.TypeParameter_Unconstrained)) },
                            accessorName: "Value"),
                        new UnionCase(
                            name: "None",
                            type: new UnionValueType("UnionTypes.Toolkit.None", TypeKind.Class, "Singleton"),
                            tagValue: 0,
                            factoryName:"None",
                            factoryParameters: null,
                            accessorKind: CaseAccessorKind.None)
                    },
                    UnionOptions.Default
                        .WithUseToolkit(true)
                        .WithGenerateMatch(true)
                        .WithGenerateEquality(true)
                        .WithGenerateToString(true)
                    ),
                namespaceName: "UnionTypes.Toolkit"
                );
        }


        [TestMethod]
        public void TestTypeUnion_CatOrDog()
        {
            var dogType = new UnionValueType("Dog", TypeKind.DecomposableLocalRecordStruct);
            var catType = new UnionValueType("Cat", TypeKind.DecomposableLocalRecordStruct);

            TestGenerate(
                new Union(
                    UnionKind.TypeUnion,
                    name: "DogOrCat",
                    typeName: "DogOrCat",
                    modifiers: "public",
                    [
                        new UnionCase(
                            name: "Dog",
                            type: dogType,
                            tagValue: -1,
                            factoryName:"CreateDog",
                            factoryParameters: [
                                new UnionCaseValue("dog", dogType, [new UnionCaseValue("name", UnionValueType.String)])
                                ]),
                        new UnionCase(
                            name: "Cat",
                            type: catType,
                            tagValue: -1,
                            factoryName: "CreateCat",
                            factoryParameters: [
                                new UnionCaseValue("cat", catType, [new UnionCaseValue("name", UnionValueType.String)])
                                ]),
                    ],
                    UnionOptions.Default.WithShareReferenceFields(false)
                    ),
                namespaceName: "TestUnions",
                usings: ["System", "System.Collections.Generic", "UnionTypes.Toolkit"]
                );
        }
#endif        

        private void TestGenerate(UnionInfo union)
        {
            var generator = new CustomUnionGenerator();
            var actualText = generator.Generate(union);
        }
    }
}