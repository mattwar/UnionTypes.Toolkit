﻿// <#+
#if !T4
namespace UnionTypes.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#endif

#nullable enable

    #region public union descriptors

    public class Union : IEquatable<Union>
    {
        /// <summary>
        /// The kind of union; Type or Tag.
        /// </summary>
        public UnionKind Kind { get; }

        /// <summary>
        /// The name of the union type (w/o type parameters).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name of the union type (with type parameters)
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// All the modifiers, including accessibility.
        /// </summary>
        public string Modifiers { get; }

        /// <summary>
        /// The options that control source generation for the union type.
        /// </summary>
        public UnionOptions Options { get; }

        /// <summary>
        /// The cases of the union type.
        /// </summary>
        public IReadOnlyList<UnionCase> Cases { get; }

        public Union(
            UnionKind kind,
            string name, 
            string typeName,
            string modifiers,
            IReadOnlyList<UnionCase> cases,
            UnionOptions options)
        {
            this.Kind = kind;
            this.Name = name;
            this.TypeName = typeName;
            this.Modifiers = modifiers;
            this.Cases = cases;
            this.Options = options;
        }

        public bool Equals(Union other)
        {
            return this.Kind == other.Kind
                && this.Name == other.Name
                && this.TypeName == other.TypeName
                && this.Modifiers == other.Modifiers
                && this.Options.Equals(other.Options)
                && this.Cases.SequenceEqual(other.Cases);
        }

        public override bool Equals(object obj) =>
            obj is Union other && Equals(other);

        public override int GetHashCode() =>
            this.Name.GetHashCode();
    }

    public enum UnionKind
    {
        /// <summary>
        /// A union of individual type values
        /// </summary>
        TypeUnion,

        /// <summary>
        /// A union of different states with optional parameters
        /// </summary>
        TagUnion
    }

    /// <summary>
    /// A set of options that control source generation for the union type.
    /// </summary>
    public class UnionOptions : IEquatable<UnionOptions>
    {
        /// <summary>
        /// Allow data fields of the same type are shared across all cases.
        /// </summary>
        public bool ShareSameTypeFields { get; }

        /// <summary>
        /// Allow data fields of reference type be shared across all cases.
        /// </summary>
        public bool ShareReferenceFields { get; }

        /// <summary>
        /// Allow structs with only overlappable fields to be overlapped with other case data.
        /// </summary>
        public bool OverlapStructs { get; }

        /// <summary>
        /// Allow structs declared outside the current compilation unit to be overlapped with other case data.
        /// </summary>
        public bool OverlapForeignStructs { get; }

        /// <summary>
        /// Decompose decomposable structs (records and tuples) to allow better data compaction.
        /// </summary>
        public bool DecomposeStructs { get; }

        /// <summary>
        /// Allow structs defined outside the compilation unit to be decomposed.
        /// </summary>
        public bool DecomposeForeignStructs { get; }

        /// <summary>
        /// Generate pass-through equality for the union type.
        /// </summary>
        public bool GenerateEquality { get; }

        /// <summary>
        /// Generate pass-through ToString implementation.
        /// </summary>
        public bool GenerateToString { get; }

        /// <summary>
        /// Generate Match methods that force handling of all cases.
        /// </summary>
        public bool GenerateMatch { get; }

        /// <summary>
        /// Use toolkit API's to enhance the union type.
        /// </summary>
        public bool UseToolkit { get; }

        /// <summary>
        /// The name of the generated tag enum.
        /// </summary>
        public string TagTypeName { get; }

        /// <summary>
        /// The name of the tag property on the union type.
        /// </summary>
        public string TagPropertyName { get; }

        private UnionOptions(
            bool shareFields,
            bool shareReferenceFields,
            bool overlapStructs,
            bool overlapForeignStructs,
            bool decomposeStructs,
            bool decomposeForeignStructs,
            bool generateEquality,
            bool generateToString,
            bool generateMatch,
            bool useToolkit,
            string tagTypeName,
            string tagPropertyName)
        {
            this.ShareSameTypeFields = shareFields;
            this.ShareReferenceFields = shareReferenceFields;
            this.OverlapStructs = overlapStructs;
            this.OverlapForeignStructs = overlapForeignStructs;
            this.DecomposeStructs = decomposeStructs;
            this.DecomposeForeignStructs = decomposeForeignStructs;
            this.GenerateEquality = generateEquality;
            this.GenerateToString = generateToString;
            this.GenerateMatch = generateMatch;
            this.UseToolkit = useToolkit;
            this.TagTypeName = tagTypeName;
            this.TagPropertyName = tagPropertyName;
        }

        public UnionOptions WithShareFields(bool share) =>
            With(shareFields: share);

        public UnionOptions WithShareReferenceFields(bool share) =>
            With(shareReferenceFields: share);

        public UnionOptions WithOverlapStructs(bool overlap) =>
            With(overlapStructs: overlap);

        public UnionOptions WithDecomposeStructs(bool decompose) =>
            With(decomposeStructs: decompose);

        public UnionOptions WithDecomposeForeignStructs(bool decompose) =>
            With(decomposeForeignStructs: decompose);

        public UnionOptions WithOverlapForeignStructs(bool overlap) =>
            With(overlapForeignStructs: overlap);

        public UnionOptions WithGenerateEquality(bool generate) =>
            With(generateEquality: generate);

        public UnionOptions WithGenerateToString(bool generate) =>
            With(generateToString: generate);

        public UnionOptions WithGenerateMatch(bool generate) =>
            With(generateMatch: generate);

        public UnionOptions WithUseToolkit(bool use) =>
            With(useToolkit: use);

        public UnionOptions WithTagTypeName(string name) =>
            With(tagTypeName: name);

        public UnionOptions WithTagPropertyName(string name) =>
            With(tagPropertyName: name);

        private UnionOptions With(
            bool? shareFields = null,
            bool? shareReferenceFields = null,
            bool? overlapStructs = null,
            bool? overlapForeignStructs = null,
            bool? decomposeStructs = null,
            bool? decomposeForeignStructs = null,
            bool? generateEquality = null,
            bool? generateToString = null,
            bool? generateMatch = null,
            bool? useToolkit = null,
            string? tagTypeName = null,
            string? tagPropertyName = null)
        {
            var newShareFields = shareFields ?? this.ShareSameTypeFields;
            var newShareReferenceFields = shareReferenceFields ?? this.ShareReferenceFields;
            var newOverlapStructs = overlapStructs ?? this.OverlapStructs;
            var newDecomposeStructs = decomposeStructs ?? this.DecomposeStructs;
            var newDecomposeForeignStructs = decomposeForeignStructs ?? this.DecomposeForeignStructs;
            var newOverlapForeignStructs = overlapForeignStructs ?? this.OverlapForeignStructs;
            var newGenerateEquality = generateEquality ?? this.GenerateEquality;
            var newGenerateToString = generateToString ?? this.GenerateToString;
            var newGenerateMatch = generateMatch ?? this.GenerateMatch;
            var newUseToolkit = useToolkit ?? this.UseToolkit;
            var newTagTypeName = tagTypeName ?? this.TagTypeName;
            var newTagPropertyName = tagPropertyName ?? this.TagPropertyName;

            if (newShareFields != this.ShareSameTypeFields
                || newShareReferenceFields != this.ShareReferenceFields
                || newOverlapStructs != this.OverlapStructs
                || newDecomposeStructs != this.DecomposeStructs
                || newDecomposeForeignStructs != this.DecomposeForeignStructs
                || newOverlapForeignStructs != this.OverlapForeignStructs
                || newGenerateEquality != this.GenerateEquality
                || newGenerateToString != this.GenerateToString
                || newGenerateMatch != this.GenerateMatch
                || newUseToolkit != this.UseToolkit
                || newTagTypeName != this.TagTypeName
                || newTagPropertyName != this.TagPropertyName)
            {
                return new UnionOptions(
                    shareFields: newShareFields,
                    shareReferenceFields: newShareReferenceFields,
                    overlapStructs: newOverlapStructs,
                    overlapForeignStructs: newOverlapForeignStructs,
                    decomposeStructs: newDecomposeStructs,
                    decomposeForeignStructs: newDecomposeForeignStructs,
                    generateEquality: newGenerateEquality,
                    generateToString: newGenerateToString,
                    generateMatch: newGenerateMatch,
                    useToolkit: newUseToolkit,
                    tagTypeName: newTagTypeName,
                    tagPropertyName: newTagPropertyName
                    );
            }

            return this;
        }

        public bool Equals(UnionOptions other)
        {
            return this.ShareSameTypeFields == other.ShareSameTypeFields
                && this.ShareSameTypeFields == other.ShareReferenceFields
                && this.OverlapStructs == other.OverlapStructs
                && this.OverlapForeignStructs == other.OverlapForeignStructs
                && this.DecomposeStructs == other.DecomposeStructs
                && this.DecomposeForeignStructs == other.DecomposeForeignStructs
                && this.GenerateEquality == other.GenerateEquality
                && this.GenerateMatch == other.GenerateMatch
                && this.GenerateToString == other.GenerateToString
                && this.UseToolkit == other.UseToolkit
                && this.TagTypeName == other.TagTypeName
                && this.TagPropertyName == other.TagPropertyName;
        }

        public override bool Equals(object obj) => 
            obj is UnionOptions other && Equals(other);

        public override int GetHashCode()
        {
            return (this.ShareSameTypeFields ? 1 : 0)
                + (this.ShareReferenceFields ? 1 : 0)
                + (this.OverlapStructs ? 1 : 0)
                + (this.OverlapForeignStructs ? 1 : 0)
                + (this.DecomposeStructs ? 1: 0)
                + (this.DecomposeForeignStructs ? 1 : 0)
                + (this.GenerateEquality ? 1 : 0)
                + (this.GenerateToString ? 1 : 0)
                + (this.GenerateMatch ? 1 : 0)
                + (this.UseToolkit ? 1 : 0)
                + (this.TagTypeName?.GetHashCode() ?? 0)
                + (this.TagPropertyName?.GetHashCode() ?? 0);
        }

        public static UnionOptions Default =
            new UnionOptions(
                shareFields: true, 
                shareReferenceFields: true, 
                overlapStructs: true, 
                overlapForeignStructs: false,
                decomposeStructs: true,
                decomposeForeignStructs: true,
                generateEquality: true,
                generateToString: true,
                generateMatch: true,
                useToolkit: true,
                tagTypeName: "Case",
                tagPropertyName: "Kind"
                );
    }

    public class UnionCase : IEquatable<UnionCase>
    {
        /// <summary>
        /// The name of the case.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the case (if type union).
        /// </summary>
        public UnionValueType? Type { get; }

        /// <summary>
        /// The value to use for the tag for this case.
        /// </summary>
        public int TagValue { get; } 

        /// <summary>
        /// The name of the factory used to construct this case.
        /// </summary>
        public string? FactoryName { get; }

        /// <summary>
        /// The parameters of the factory used to construct this case.
        /// </summary>
        public IReadOnlyList<UnionCaseValue> FactoryParameters { get; }

        /// <summary>
        /// The kind of factory for this case: method or property.
        /// </summary>
        public CaseFactoryKind FactoryKind { get; }

        /// <summary>
        /// The modifiers of the user declared factory.
        /// </summary>
        public string? FactoryModifiers { get; }

        /// <summary>
        /// The name of the accessor property for this case.
        /// </summary>
        public string? AccessorName { get; }

        /// <summary>
        /// The kind of accessor for this case: none, property, method, outmethod.
        /// </summary>
        public CaseAccessorKind AccessorKind { get; }    

        /// <summary>
        /// The accessibilty of generated members.
        /// </summary>
        public string? Accessibility { get; }
 
        public UnionCase(
            string name,
            UnionValueType? type = null,
            int tagValue = -1,
            string? factoryName = null,
            IReadOnlyList<UnionCaseValue>? factoryParameters = null,
            CaseFactoryKind factoryKind = CaseFactoryKind.Method,
            string? factoryModifiers = null,
            string? accessorName = null,
            CaseAccessorKind accessorKind = CaseAccessorKind.Property,
            string? accessibility = null)
        {
            this.Name = name;
            this.Type = type;
            this.TagValue = tagValue;
            this.FactoryName = factoryName;
            this.FactoryParameters = factoryParameters ?? Array.Empty<UnionCaseValue>();
            this.FactoryKind = factoryKind;
            this.FactoryModifiers = factoryModifiers;
            this.AccessorName = accessorName;
            this.AccessorKind = accessorKind;
            this.Accessibility = accessibility;
        }

        public bool Equals(UnionCase other)
        {
            return this.Name == other.Name
                && (this.Type == other.Type || this.Type != null && other.Type != null && this.Type.Equals(other.Type))
                && this.TagValue == other.TagValue
                && this.FactoryName == other.FactoryName
                && this.FactoryKind == other.FactoryKind
                && this.FactoryModifiers == other.FactoryModifiers
                && this.AccessorName == other.AccessorName
                && this.AccessorKind == other.AccessorKind
                && this.Accessibility == other.Accessibility
                && this.FactoryParameters.SequenceEqual(other.FactoryParameters);
        }

        public override bool Equals(object obj) =>
            obj is UnionCase other && Equals(other);

        public override int GetHashCode() =>
            this.Name.GetHashCode();
    }

    public enum CaseFactoryKind
    {
        Method,
        None,
        Property
    }

    public enum CaseAccessorKind
    {
        Method,
        None,
        Property
    }

    /// <summary>
    /// The declaration of an individual union case value.
    /// </summary>
    public class UnionCaseValue : IEquatable<UnionCaseValue>
    {
        /// <summary>
        /// The name of the case value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the case value.
        /// </summary>
        public UnionValueType Type { get; }

        /// <summary>
        /// The members of the value it can be decomposed into.
        /// </summary>
        public IReadOnlyList<UnionCaseValue> Members { get; }

        public UnionCaseValue(
            string name,
            UnionValueType type,
            IReadOnlyList<UnionCaseValue>? members = null)
        {
            this.Name = name;
            this.Type = type;
            this.Members = members ?? Array.Empty<UnionCaseValue>();
        }

        public bool Equals(UnionCaseValue other)
        {
            return this.Name == other.Name
                && this.Type.Equals(other.Type)
                && this.Members.Count == other.Members.Count
                && this.Members.SequenceEqual(other.Members);
        }

        public override bool Equals(object obj) =>
            obj is UnionCaseValue other && Equals(other);

        public override int GetHashCode() =>
            this.Name.GetHashCode();
    }

    public class UnionValueType : IEquatable<UnionValueType>
    {
        /// <summary>
        /// The full name of the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The kind of the type.
        /// </summary>
        public TypeKind Kind { get; }

        /// <summary>
        /// The name of the static field/property of the type that accesses the singleton value.
        /// </summary>
        public string? SingletonAccessor { get; }

        /// <summary>
        /// True if the type is a singleton.
        /// </summary>
        public bool IsSingleton => this.SingletonAccessor != null;

        public UnionValueType(string typeName, TypeKind kind, string? singletonAccessor = null)
        {
            this.Name = typeName;
            this.Kind = kind;
            this.SingletonAccessor = singletonAccessor;
        }

        public bool Equals(UnionValueType other)
        {
            return this.Name == other.Name
                && this.Kind == other.Kind
                && this.SingletonAccessor == other.SingletonAccessor;
        }

        public static readonly UnionValueType String = new UnionValueType("string", TypeKind.Class);
        public static readonly UnionValueType Int32 = new UnionValueType("int", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType Int16 = new UnionValueType("short", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType Int64 = new UnionValueType("long", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType UInt32 = new UnionValueType("uint", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType UInt16 = new UnionValueType("ushort", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType UInt64 = new UnionValueType("ulong", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType Decimal = new UnionValueType("decimal", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType Single = new UnionValueType("float", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType Double = new UnionValueType("double", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType Byte = new UnionValueType("byte", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType SByte = new UnionValueType("sbyte", TypeKind.PrimitiveStruct);
        public static readonly UnionValueType Bool = new UnionValueType("bool", TypeKind.PrimitiveStruct); 
        public static readonly UnionValueType Object = new UnionValueType("object", TypeKind.Object);
    }

    /// <summary>
    /// The kind of type the case value represents.
    /// Useful for determining how the value can be stored in the union's data fields.
    /// </summary>
    public enum TypeKind
    {
        /// <summary>
        /// The kind is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// A record struct defined within the current compilation unit.
        /// </summary>
        DecomposableLocalRecordStruct,

        /// <summary>
        /// A record struct defined outside the current compilation unit.
        /// </summary>
        DecomposableForeignRecordStruct,

        /// <summary>
        /// struct containing one or more reference values and one or more value-type values
        /// </summary>
        NonOverlappableStruct,

        /// <summary>
        /// struct containing only overlappable field members,
        /// declared within the current compilation unit.
        /// </summary>
        OverlappableLocalStruct,

        /// <summary>
        /// a record struct containing only overlappable field members,
        /// declared within the current compilation unit.
        /// </summary>
        OverlappableLocalRecordStruct,

        /// <summary>
        /// A local record struct that is both overlappable and decomposable
        /// </summary>
        OverlappableDecomposableLocalRecordStruct,

        /// <summary>
        /// A struct containing only overlappable field members, 
        /// not declared within the current compilation unit.
        /// </summary>
        OverlappableForeignStruct,

        /// <summary>
        /// a record struct containing only overlappable field members,
        /// not declared within the current compilation unit.
        /// </summary>
        OverlappableForeignRecordStruct,

        /// <summary>
        /// A foreign record struct that is both overlappable and decomposable.
        /// </summary>
        OverlappableDecomposableForeignRecordStruct,

        /// <summary>
        /// A value tuple with all overlappable members
        /// </summary>
        OverlappableTuple,

        /// <summary>
        /// A tuple that cannot be overlapped, but it can be decomposed.
        /// </summary>
        NonOverlappableTuple,

        /// <summary>
        /// Just a single overlappable primitive value type
        /// </summary>
        PrimitiveStruct,

        /// <summary>
        /// A ref-like struct
        /// </summary>
        RefStruct,

        /// <summary>
        /// class containing zero or more values
        /// </summary>
        Class,

        /// <summary>
        /// An interface
        /// </summary>
        Interface,

        /// <summary>
        /// The object type
        /// </summary>
        Object,

        /// <summary>
        /// A type parameter
        /// </summary>
        TypeParameter_Unconstrained,

        /// <summary>
        /// A ref constrained type parameter
        /// </summary>
        TypeParameter_RefConstrained,

        /// <summary>
        /// A value-type constrained type parameter
        /// </summary>
        TypeParameter_ValConstrained
    }
    #endregion

    /// <summary>
    /// Generates the source code for a union type, given its description.
    /// </summary>
    public class UnionGenerator : Generator
    {
        private string? _namespace;
        private readonly IReadOnlyList<string>? _usings;

        public UnionGenerator(
            string? namespaceName = null, 
            IReadOnlyList<string>? usings = null
            )
        {
            _namespace = namespaceName;
            _usings = usings;
        }

        public static string ToolkitNamespace = "UnionTypes.Toolkit";

        public static string Generate()
        {
            return "";
        }

        public static string Generate(Union union, string? namespaceName = null, string[]? usings = null)
        {
            return new UnionGenerator(namespaceName, usings).GenerateFile(union);
        }

        public string GenerateFile(params Union[] unions)
        {
            var infos = unions.Select(u => CreateUnionLayout(u)).ToList();
            this.WriteFile(infos);
            return this.GeneratedText;
        }

        #region layout types

        /// <summary>
        /// The field layout and generation plan for the union type.
        /// </summary>
        private class UnionLayout
        {
            /// <summary>
            /// The original description of the union.
            /// </summary>
            public Union Union { get; }

            /// <summary>
            /// The layouts of the individual union cases.
            /// </summary>
            public IReadOnlyList<UnionCaseLayout> Cases { get; }

            /// <summary>
            /// All non-overlapped fields in the union type.
            /// </summary>
            public IReadOnlyList<DataField> NonOverlappedFields { get; }

            /// <summary>
            /// The field in the union type that holds overlapped data.
            /// </summary>
            public DataField? OverlappedDataField { get; }

            public UnionOptions Options { get; }

            public UnionKind Kind => this.Union.Kind;
            public string Name => this.Union.Name;
            public string TypeName => this.Union.TypeName;
            public string Modifiers => this.Union.Modifiers;

            public UnionLayout(
                Union union,
                IReadOnlyList<UnionCaseLayout> cases,
                IReadOnlyList<DataField> nonOverlapedFields,
                DataField? overlappedDataField,
                UnionOptions? options = null)
            {
                this.Union = union;
                this.Cases = cases;
                this.NonOverlappedFields = nonOverlapedFields;
                this.OverlappedDataField = overlappedDataField;
                this.Options = options ?? union.Options;
            }
        }

        /// <summary>
        /// The field layout and generation plan for the individual union cases.
        /// </summary>
        private class UnionCaseLayout
        {
            /// <summary>
            /// The description of the case.
            /// </summary>
            public UnionCase Case { get; }

            /// <summary>
            /// The layout for the parameters to the factory method for the case.
            /// </summary>
            public IReadOnlyList<CaseValueLayout> FactoryParameters { get; }

            /// <summary>
            /// The type of the case, if the union is a type union.
            /// </summary>
            public UnionValueType? Type { get; }

            /// <summary>
            /// The value used for the tag associated with the case.
            /// </summary>
            public int TagValue { get; }

            /// <summary>
            /// The field in the union's overlapped data that holds either
            /// a single value or a struct with all the case's overlappable fields.
            /// </summary>
            public DataField? OverlappedCaseField { get; }

            /// <summary>
            /// The individual fields for the overlapped data for the case,
            /// if more than one parameter is overlappable.
            /// </summary>
            public IReadOnlyList<DataField> OverlappedCaseDataFields { get; }

            public string Name => this.Case.Name;
            public CaseFactoryKind FactoryKind => this.Case.FactoryKind;
            public string FactoryModifiers => this.Case.FactoryModifiers ?? $"{this.Accessibility} static";
            public CaseAccessorKind AccessorKind => this.Case.AccessorKind;
            public string Accessibility => this.Case.Accessibility ?? "public";

            public UnionCaseLayout(
                UnionCase unionCase,
                IReadOnlyList<CaseValueLayout> factoryParameters,
                UnionValueType? type,
                int tagValue,
                DataField? overlappedCaseField,
                IReadOnlyList<DataField> overlappedCaseDataFields)
            {
                this.Case = unionCase;
                this.FactoryParameters = factoryParameters;
                this.Type = type;
                this.TagValue = tagValue;
                this.OverlappedCaseField = overlappedCaseField;
                this.OverlappedCaseDataFields = overlappedCaseDataFields;
            }

            /// <summary>
            /// Gets the parameter associated with the field.
            /// </summary>
            public bool TryGetParameter(DataField field, out CaseValueLayout parameter)
            {
                foreach (var p in this.FactoryParameters)
                {
                    if (p.Field == field)
                    {
                        parameter = p;
                        return true;
                    }
                    else if (p.Members.Count > 0
                        && TryGetNestedParameter(p.Members, field, out parameter))
                    {
                        return true;
                    }
                }

                parameter = default!;
                return false;

                bool TryGetNestedParameter(IReadOnlyList<CaseValueLayout> ps, DataField field, out CaseValueLayout parameter)
                {
                    foreach (var np in ps)
                    {
                        if (np.Field == field)
                        {
                            parameter = np;
                            return true;
                        }
                        if (TryGetNestedParameter(np.Members, field, out parameter))
                            return true;
                    }
                    parameter = default!;
                    return false;
                }
            }
        }

        /// <summary>
        /// The field layout and generation plan for a case value.
        /// </summary>
        private class CaseValueLayout
        {
            public UnionCaseValue Value { get; }
            public string? PathFromFactoryArg { get; }
            public DataField? Field { get; }
            public DataKind DataKind { get; }
            public IReadOnlyList<CaseValueLayout> Members { get; }

            public CaseValueLayout(
                UnionCaseValue value,
                DataKind dataKind,
                DataField? field,
                string? pathFromFactoryArg, 
                IReadOnlyList<CaseValueLayout>? members)
            {
                this.Value = value;
                this.DataKind = dataKind;
                this.Field = field;
                this.PathFromFactoryArg = pathFromFactoryArg;
                this.Members = members ?? Array.Empty<CaseValueLayout>();
            }

            public string Name => this.Value.Name;
            public UnionValueType Type => this.Value.Type;
        }

        /// <summary>
        /// And individual data field on the union type,
        /// or one of the overlapped types.
        /// </summary>
        private class DataField
        {
            public DataKind DataKind { get; }

            /// <summary>
            /// The name of the field.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The type of this field.
            /// </summary>
            public string TypeName { get; set; }

            /// <summary>
            /// The name of the constructor argument corresponding to this field,
            /// when this is a field of the union type.
            /// </summary>
            public string? ConstructorArg { get; set; }

            public DataField(
                DataKind kind,
                string name,
                string type,
                string? constructorArg)
            {
                this.DataKind = kind;
                this.Name = name;
                this.TypeName = type;
                this.ConstructorArg = constructorArg;
            }
        }

        /// <summary>
        /// The kind of data a case parameter represents.
        /// </summary>
        public enum DataKind
        {
            /// <summary>
            /// Any type that can share the same field with a parameter from a difference case
            /// with the same type.
            /// </summary>
            SameTypeSharable,

            /// <summary>
            /// Any reference type that can share the same field with any other reference type,
            /// with the field typed as object.
            /// </summary>
            ReferenceSharable,

            /// <summary>
            /// Any type that can be decomposed into separate values; ValueTuple or record struct.
            /// </summary>
            Decomposable,

            /// <summary>
            /// Any type that can be overlapped with other overlappable types.
            /// </summary>
            Overlappable,

            /// <summary>
            /// Any type that must have its own unique field.
            /// </summary>
            Unique
        }

        #endregion

        #region create layout plan

        private UnionLayout CreateUnionLayout(Union union)
        {
            if (union.Options.OverlapStructs)
            {
                // only use overlap fields if the number of fields is less than without overlap.
                // this can occur when overlappable fields also type match with other fields.
                var layoutNoOverlap = CreateUnionLayout(union, allowOverlappedFields: false);
                var layoutWithOverlap = CreateUnionLayout(union, allowOverlappedFields: true);

                var fieldsNoOverlap = layoutNoOverlap.NonOverlappedFields.Count + (layoutNoOverlap.OverlappedDataField != null ? 1 : 0);
                var fieldsWithOverlap = layoutWithOverlap.NonOverlappedFields.Count + (layoutWithOverlap.OverlappedDataField != null ? 1 : 0);

                return fieldsWithOverlap < fieldsNoOverlap
                    ? layoutWithOverlap
                    : layoutNoOverlap;
            }
            else
            {
                return CreateUnionLayout(union, allowOverlappedFields: false);
            }
        }

        private UnionLayout CreateUnionLayout(Union union, bool allowOverlappedFields)
        {
            var numberOfCasesWithOverlappableData = allowOverlappedFields 
                ? GetOverlappedCaseCount(union) 
                : 0;

            var unionFields = new List<DataField>();
            var cases = new List<UnionCaseLayout>();

            // only have overlapped data if more than one case has overlappable data
            DataField? overlappedUnionField = numberOfCasesWithOverlappableData > 1
                ? new DataField(DataKind.Unique, "_overlapped", "OverlappedData", "overlapped")
                : null;

            var usedTagValues = new SortedSet<int>(union.Cases.Where(c => c.TagValue >= 0).Select(c => c.TagValue));

            var options = union.Options;
            if (union.Modifiers.Contains("ref"))
            {
                options = options
                    .WithGenerateEquality(false)
                    //.WithGenerateMatch(false)
                    .WithGenerateToString(false)
                    .WithUseToolkit(false);
            }

            foreach (var unionCase in union.Cases)
            {
                var allocatedFields = new HashSet<DataField>();

                // if the case has move than one overlapped data field, then create a unique field for the case
                DataField? overlappedCaseField = (overlappedUnionField != null
                    && GetOverlappedCaseDataFieldCount(union, unionCase) > 1)
                    ? new DataField(DataKind.Unique, unionCase.Name, unionCase.Name + "Data", null)
                    : null;

                var factoryParamLayouts = new List<CaseValueLayout>();
                foreach (var param in unionCase.FactoryParameters)
                {
                    var caseParam = CreateCaseValueLayout(
                        union,
                        unionCase,
                        param,
                        unionFields,
                        allocatedFields,
                        overlappedUnionField,
                        overlappedCaseField,
                        parentPath: "",
                        parentName: "",
                        pathFromFactoryArg: ""
                        );
                    factoryParamLayouts.Add(caseParam);
                }

                var overlappedCaseDataFields = GetOverlappedFields(factoryParamLayouts);

                if (overlappedCaseField == null 
                    && overlappedCaseDataFields.Count == 1)
                {
                    // if there is only one overlapped case data field then use it as the case field
                    overlappedCaseField = overlappedCaseDataFields[0];
                    overlappedCaseDataFields = Array.Empty<DataField>();
                }

                var type = union.Kind == UnionKind.TypeUnion && factoryParamLayouts.Count == 1
                    ? factoryParamLayouts[0].Type
                    : unionCase.Type;

                var tagValue = unionCase.TagValue;
                if (tagValue < 0)
                    tagValue = GetNextTagValue(usedTagValues);
                usedTagValues.Add(tagValue);

                var info = new UnionCaseLayout(
                    unionCase,
                    factoryParamLayouts,
                    type,
                    tagValue,
                    overlappedCaseField,
                    overlappedCaseDataFields
                    );

                cases.Add(info);
            }

            return new UnionLayout(
                union,
                cases,
                unionFields,
                overlappedUnionField,
                options
                );
        }

        private int GetNextTagValue(SortedSet<int> usedTagValues)
        {
            // start at 1
            if (usedTagValues.Count == 0)
                return 1;

            // look for gap in already used values           
            var lastTagValue = -1;

            foreach (var tagValue in usedTagValues)
            {
                if (lastTagValue >= 0 && tagValue > lastTagValue + 1)
                    break;
                lastTagValue = tagValue;
            }

            return lastTagValue + 1;
        }


        /// <summary>
        /// Gets the list overlapped fields in the value layouts.
        /// </summary>
        private static IReadOnlyList<DataField> GetOverlappedFields(IEnumerable<CaseValueLayout> layouts)
        {
            var overlappedFields = new List<DataField>();
            GetOverlappedFields(layouts);
            return overlappedFields;

            void GetOverlappedFields(IEnumerable<CaseValueLayout> layouts)
            {
                foreach (var layout in layouts)
                {
                    if (layout.Field != null && layout.Field.DataKind == DataKind.Overlappable)
                    {
                        overlappedFields.Add(layout.Field);
                    }

                    GetOverlappedFields(layout.Members);
                }
            }
        }

        /// <summary>
        /// Gets the number of cases that have overlapped case values.
        /// </summary>
        private int GetOverlappedCaseCount(Union union)
        {
            var caseCount = 0;

            foreach (var unionCase in union.Cases)
            {
                if (GetOverlappedCaseDataFieldCount(union, unionCase) > 0)
                    caseCount++;    
            }

            return caseCount;
        }

        /// <summary>
        /// Gets the number of fields an individual case requires.
        /// </summary>
        private int GetOverlappedCaseDataFieldCount(
            Union union, UnionCase unionCase)
        {
            return CountOverlappedFields(unionCase.FactoryParameters);

            int CountOverlappedFields(IReadOnlyList<UnionCaseValue> caseValues)
            {
                int count = 0;
                foreach (var caseValue in caseValues)
                {
                    var kind = GetDataKind(union.Options, caseValue.Type.Kind);
                    if (kind == DataKind.Overlappable)
                    {
                        count++;
                    }
                    else if (kind == DataKind.Decomposable)
                    {
                        count += CountOverlappedFields(caseValue.Members);
                    }
                }
                return count;
            }
        }

        private CaseValueLayout CreateCaseValueLayout(
            Union union,
            UnionCase unionCase,
            UnionCaseValue caseValue,
            List<DataField> unionFields,
            HashSet<DataField> allocatedUnionFields,
            DataField? overlappedUnionField,
            DataField? overlappedCaseField,
            string parentPath,
            string parentName,
            string pathFromFactoryArg)
        {
            DataField? field = null;
            List<CaseValueLayout>? memberLayouts = null;
            var dataKind = GetDataKind(union.Options, caseValue.Type.Kind, overlappedUnionField != null);

            pathFromFactoryArg = CombinePath(pathFromFactoryArg, caseValue.Name);

            switch (dataKind)
            {
                case DataKind.Decomposable:
                    var isFactoryParameter = unionCase.FactoryParameters.Contains(caseValue);
                    memberLayouts = new List<CaseValueLayout>(caseValue.Members.Count);
                    foreach (var np in caseValue.Members)
                    {
                        // ignore initial parent name for type case (since it will be 'value')
                        var newParentName = isFactoryParameter && union.Kind == UnionKind.TypeUnion
                            ? parentName
                            : CombineName(parentName, caseValue.Name);

                        var newMemberLayout = CreateCaseValueLayout(
                            union,
                            unionCase,
                            np,
                            unionFields,
                            allocatedUnionFields,
                            overlappedUnionField,
                            overlappedCaseField,
                            parentPath,
                            newParentName,
                            pathFromFactoryArg
                            );

                        memberLayouts.Add(newMemberLayout);
                    }
                    break;

                case DataKind.Overlappable:
                    {
                        // the name of the field includes all the name of the 
                        // containing case values, so that the field name is unique.
                        var name = CombineName(parentName, caseValue.Name);

                        // if their is not case field for this case
                        // then add the name of the case to the field
                        if (overlappedCaseField == null)
                        {
                            name = CombineName(unionCase.Name, name);
                        }

                        field = new DataField(DataKind.Overlappable, name, caseValue.Type.Name, null);
                    }
                    break;

                case DataKind.SameTypeSharable:
                case DataKind.ReferenceSharable:
                case DataKind.Unique:
                    {
                        string name = CombineName(unionCase.Name, caseValue.Name);
                        field = FindOrAllocateUnionField(
                            dataKind,
                            name,
                            caseValue.Type,
                            unionFields,
                            allocatedUnionFields
                            );
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            return new CaseValueLayout(
                caseValue,
                dataKind,
                field,
                pathFromFactoryArg,
                memberLayouts
                );
        }

        private DataField FindOrAllocateUnionField(
            DataKind dataKind,
            string name,
            UnionValueType type,
            List<DataField> unionFields,
            HashSet<DataField> allocatedUnionFields)
        {
            // find existing unallocated union field.
            for (int i = 0; i < unionFields.Count; i++)
            {
                var field = unionFields[i];
                if (!allocatedUnionFields.Contains(field))
                {
                    if (dataKind == DataKind.SameTypeSharable 
                        && field.TypeName == type.Name)
                    {
                        field.ConstructorArg = "shared" + (i + 1);
                        field.Name = "_data_" + field.ConstructorArg;
                        allocatedUnionFields.Add(field);
                        return field;
                    }
                    else if (dataKind == DataKind.ReferenceSharable 
                        && field.DataKind == DataKind.ReferenceSharable)
                    {
                        // convert field type to object when it becomes shared and the types are different
                        if (field.TypeName != type.Name && field.TypeName != "object")
                        {
                            field.TypeName = "object";
                        }

                        field.ConstructorArg = "shared" + (i + 1);
                        field.Name = "_data_" + field.ConstructorArg;
                        allocatedUnionFields.Add(field);
                        return field;
                    }
                }
            }

            var argName = LowerName(name);
            var newFieldName = "_data_" + argName;
            var newField = new DataField(dataKind, newFieldName, type.Name, argName);

            unionFields.Add(newField);
            allocatedUnionFields.Add(newField);
            return newField;
        }

        private DataKind GetDataKind(UnionOptions options, TypeKind kind, bool allowOverlappingData = true)
        {
            var dataKind = GetBaseKind(kind);
            
            if (dataKind == DataKind.Overlappable && (!options.OverlapStructs || !allowOverlappingData))
                dataKind = DataKind.SameTypeSharable;
            if (dataKind == DataKind.Decomposable && !options.DecomposeStructs)
                dataKind = DataKind.SameTypeSharable;
            if (dataKind == DataKind.ReferenceSharable && !options.ShareReferenceFields)
                dataKind = DataKind.SameTypeSharable;
            if (dataKind == DataKind.SameTypeSharable && !options.ShareSameTypeFields)
                dataKind = DataKind.Unique;

            return dataKind;

            DataKind GetBaseKind(TypeKind kind)
            {
                switch (kind)
                {
                    case TypeKind.TypeParameter_Unconstrained:
                    case TypeKind.TypeParameter_ValConstrained:
                    case TypeKind.NonOverlappableStruct:
                    case TypeKind.Unknown:
                        return DataKind.SameTypeSharable;

                    case TypeKind.NonOverlappableTuple:
                    case TypeKind.DecomposableLocalRecordStruct:
                        return DataKind.Decomposable;

                    case TypeKind.DecomposableForeignRecordStruct:
                        if (options.DecomposeForeignStructs)
                        {
                            return DataKind.Decomposable;
                        }
                        else
                        {
                            return DataKind.SameTypeSharable;
                        }

                    case TypeKind.Class:
                    case TypeKind.Interface:
                    case TypeKind.Object:
                    case TypeKind.TypeParameter_RefConstrained:
                        return DataKind.ReferenceSharable;

                    case TypeKind.PrimitiveStruct:
                    case TypeKind.OverlappableLocalStruct:
                        return DataKind.Overlappable;

                    case TypeKind.OverlappableForeignStruct:
                        if (options.OverlapForeignStructs)
                        {
                            return DataKind.Overlappable;
                        }
                        else
                        {
                            return DataKind.SameTypeSharable;
                        }

                    case TypeKind.OverlappableTuple:
                        if (options.OverlapStructs && allowOverlappingData)
                        {
                            return DataKind.Overlappable;
                        }
                        else
                        {
                            return DataKind.Decomposable;
                        }

                    case TypeKind.OverlappableDecomposableLocalRecordStruct:
                        if (options.OverlapStructs && allowOverlappingData)
                        {
                            return DataKind.Overlappable;
                        }
                        else
                        {
                            return DataKind.Decomposable;
                        }

                    case TypeKind.OverlappableDecomposableForeignRecordStruct:
                        if (options.OverlapForeignStructs && allowOverlappingData)
                        {
                            return DataKind.Overlappable;
                        }
                        else
                        {
                            return GetBaseKind(TypeKind.DecomposableForeignRecordStruct);
                        }

                    case TypeKind.OverlappableLocalRecordStruct:
                        return DataKind.Overlappable;

                    case TypeKind.OverlappableForeignRecordStruct:
                        if (options.OverlapForeignStructs)
                        {
                            return DataKind.Overlappable;
                        }
                        else
                        {
                            return DataKind.SameTypeSharable;
                        }

                    case TypeKind.RefStruct:
                        return DataKind.SameTypeSharable;

                    default:
                        return DataKind.SameTypeSharable;
                }
            }
        }

        private static bool IsPossibleReference(TypeKind kind)
        {
            switch (kind)
            {
                case TypeKind.Class:
                case TypeKind.Interface:
                case TypeKind.Object:
                case TypeKind.TypeParameter_Unconstrained:
                case TypeKind.TypeParameter_RefConstrained:
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region write source

        private static readonly List<string> _defaultUsings = new List<string>
        {
            "using System;",
            "using System.Collections.Generic;",
            "using System.Diagnostics.CodeAnalysis;",
            "using System.Runtime.InteropServices;",
        };

        private static string FormatUsing(string uzing)
        {
            return (uzing.StartsWith("using") ? uzing : $"using {uzing}")
                   + (uzing.EndsWith(";") ? "" : ";");
        }

        private void WriteFile(IReadOnlyList<UnionLayout> unions)
        {
            var allUsings = new List<string>(_defaultUsings);

            if (unions.Any(u => u.Options.UseToolkit))
            {
                allUsings.Add($"using {ToolkitNamespace};");
            }

            if (_usings != null)
            {
                foreach (var u in _usings)
                {
                    var uz = FormatUsing(u);
                    if (!allUsings.Contains(uz))
                    {
                        allUsings.Add(uz);
                    }
                }
            }

            foreach (var u in allUsings)
            {
                WriteLine(u);
            }

            WriteLine("#nullable enable");
            WriteLine();

            if (!string.IsNullOrEmpty(_namespace))
            {
                WriteLine($"namespace {_namespace}");
                WriteBraceNested(() =>
                {
                    WriteUnions(unions);
                });
            }
            else
            {
                WriteUnions(unions);
            }
        }

        private void WriteUnions(IReadOnlyList<UnionLayout> unions)
        {
            var lastUnion = unions.LastOrDefault();
            foreach (var union in unions)
            {
                WriteUnion(union);

                if (union != lastUnion)
                    Console.WriteLine();
            }
        }

        private void WriteUnion(UnionLayout union)
        {
            if (union.Kind == UnionKind.TypeUnion)
            {
                var interfaces = new List<string>();

                if (union.Options.UseToolkit && CanUseGenericTypeParameters(union))
                    interfaces.Add($"IClosedTypeUnion<{union.TypeName}>");

                if (union.Options.GenerateEquality)
                    interfaces.Add($"IEquatable<{union.TypeName}>");

                var interfaceList = string.Join(", ", interfaces);
                if (interfaceList.Length > 0)
                    interfaceList = " : " + interfaceList;

                WriteLine($"{union.Modifiers} struct {union.TypeName}{interfaceList}");
                WriteBraceNested(() =>
                {
                    WriteLineSeparated(
                        () => WriteTagDeclaration(union),
                        () => WriteFields(union),
                        () => WriteOverlappedDataTypes(union),
                        () => WriteConstructor(union),
                        () => WriteFactoryMethods(union),
                        () => WriteImplicitCastOperators(union),
                        () => WriteTryCreate(union),
                        () => WriteAccessors(union),
                        () => WriteCaseTuples(union),
                        () => WriteTryGet(union),
                        () => WriteITypeUnionMembers(union),
                        () => WriteEqualityMethods(union),
                        () => WriteToString(union),
                        () => WriteMatchMethods(union)
                        );
                });
            }
            else
            {
                var interfaces = new List<string>();
                if (union.Options.GenerateEquality)
                    interfaces.Add($"IEquatable<{union.TypeName}>");
                var interfaceList = string.Join(", ", interfaces);
                if (interfaceList.Length > 0)
                    interfaceList = " : " + interfaceList;

                WriteLine($"{union.Modifiers} struct {union.TypeName}{interfaceList}");
                WriteBraceNested(() =>
                {
                    WriteLineSeparated(
                        () => WriteTagDeclaration(union),
                        () => WriteFields(union),
                        () => WriteOverlappedDataTypes(union),
                        () => WriteConstructor(union),
                        () => WriteFactoryMethods(union),
                        () => WriteAccessors(union),
                        () => WriteCaseTuples(union),
                        () => WriteEqualityMethods(union),
                        () => WriteToString(union),
                        () => WriteMatchMethods(union)
                        );
                });
            }
        }

        private void WriteTagDeclaration(UnionLayout union)
        {
            WriteLine($"public enum {GetTagTypeName(union)}");
            WriteBraceNested(() =>
            {
                foreach (var c in union.Cases)
                {
                    WriteLine($"{GetCaseTagName(c)} = {c.TagValue},");
                }
            });
        }

        private void WriteFields(UnionLayout union)
        {
            WriteLine($"public {GetTagTypeName(union)} {GetTagPropertyName(union)} {{ get; }}");

            if (union.OverlappedDataField != null)
            {
                WriteLine($"private readonly {union.OverlappedDataField.TypeName} {union.OverlappedDataField.Name};");
            }

            foreach (var field in union.NonOverlappedFields)
            {
                WriteLine($"private readonly {field.TypeName} {field.Name};");
            }
        }

        private void WriteOverlappedDataTypes(UnionLayout union)
        {
            if (union.OverlappedDataField != null)
            {
                WriteLineSeparated(() =>
                {
                    WriteLine("[StructLayout(LayoutKind.Explicit)]");
                    WriteLine($"private struct {union.OverlappedDataField.TypeName}");
                    WriteBraceNested(() =>
                    {
                        WriteLineSeparatedBlocks(() =>
                        {
                            WriteBlock(() =>
                            {
                                foreach (var unionCase in union.Cases)
                                {
                                    if (unionCase.OverlappedCaseField != null)
                                    {
                                        WriteLine($"[FieldOffset(0)]");
                                        WriteLine($"internal {unionCase.OverlappedCaseField.TypeName} {unionCase.OverlappedCaseField.Name};");
                                    }
                                }
                            });

                        });
                    });

                    foreach (var unionCase in union.Cases)
                    {
                        if (unionCase.OverlappedCaseField != null
                            && unionCase.OverlappedCaseDataFields.Count > 0)
                        {
                            WriteBlock(() =>
                            {
                                WriteLine($"private struct {unionCase.OverlappedCaseField.TypeName}");
                                WriteBraceNested(() =>
                                {
                                    foreach (var field in unionCase.OverlappedCaseDataFields)
                                    {
                                        WriteLine($"internal {field.TypeName} {field.Name};");
                                    }
                                });
                            });
                        }
                    }
                });
            }
        }

        private void WriteConstructor(UnionLayout union)
        {
            var args = new List<string>();
            args.Add($"{GetTagTypeName(union)} {GetTagArgumentName(union)}");

            if (union.OverlappedDataField != null)
                args.Add($"{union.OverlappedDataField.TypeName} {union.OverlappedDataField.ConstructorArg}");

            foreach (var field in union.NonOverlappedFields)
            {
                args.Add($"{field.TypeName} {field.ConstructorArg}");
            }

            var argsList = string.Join(", ", args);

            WriteLine($"private {union.Name}({argsList})");
            WriteBraceNested(() =>
            {
                WriteLine($"this.{GetTagPropertyName(union)} = {GetTagArgumentName(union)};");

                if (union.OverlappedDataField != null)
                    WriteLine($"{union.OverlappedDataField.Name} = {union.OverlappedDataField.ConstructorArg};");

                foreach (var field in union.NonOverlappedFields)
                {
                    WriteLine($"{field.Name} = {field.ConstructorArg};");
                }
            });
        }

        private void WriteFactoryMethods(UnionLayout union)
        {
            foreach (var unionCase in union.Cases)
            {
                var factoryName = GetFactoryName(union, unionCase);
                var unionConstruction = GetUnionCaseConstructorExpression(union, unionCase);

                switch (unionCase.FactoryKind)
                {
                    case CaseFactoryKind.Property:
                        WriteLine($"{unionCase.FactoryModifiers} {union.TypeName} {factoryName} => {unionConstruction};");
                        break;

                    case CaseFactoryKind.Method:
                        var parameters = string.Join(", ", unionCase.FactoryParameters.Select(p => $"{p.Type.Name} {p.Name}"));
                        WriteLine($"{unionCase.FactoryModifiers} {union.TypeName} {factoryName}({parameters}) => {unionConstruction};");
                        break;
                }
            }
        }

        private void WriteImplicitCastOperators(UnionLayout union)
        {
            if (union.Kind == UnionKind.TypeUnion)
            {
                // implicit cast value to union
                foreach (var unionCase in union.Cases)
                {
                    if (unionCase.Accessibility == "public"
                        && unionCase.Type != null
                        && unionCase.Type.Kind != TypeKind.Interface
                        && unionCase.Type.Kind != TypeKind.Object)
                    {
                        WriteLine($"public static implicit operator {union.TypeName}({unionCase.Type.Name} value) => {GetFactoryCallExpression(union, unionCase, "value")};");
                    }
                }
            }
        }

        private void WriteAccessors(UnionLayout union)
        {
            foreach (var unionCase in union.Cases)
            {
                var access = unionCase.Accessibility;

                switch (unionCase.AccessorKind)
                {
                    case CaseAccessorKind.Property:
                        if (unionCase.FactoryParameters.Count == 0)
                        {
                            if (unionCase.Type != null && unionCase.Type.IsSingleton)
                            {
                                WriteLine($"{access} {unionCase.Type.Name} {GetAccessorName(union, unionCase)} => {GetTagComparison(union, unionCase)} ? {GetCaseValueConstructionExpression(union, unionCase)} : default!;");
                            }
                            else
                            {
                                WriteLine($"{access} bool {GetAccessorName(union, unionCase)} => {GetTagComparison(union, unionCase)};");
                            }
                        }
                        else if (unionCase.FactoryParameters.Count == 1)
                        {
                            var param = unionCase.FactoryParameters[0];
                            WriteLine($"{access} {param.Type.Name} {GetAccessorName(union, unionCase)} => {GetTagComparison(union, unionCase)} ? {GetCaseValueConstructionExpression(union, unionCase)} : default!;");
                        }
                        else if (unionCase.FactoryParameters.Count > 1)
                        {
                            var tupleType = GetCaseTupleTypeExpression(unionCase);
                            var tupleConstruction = GetCaseTupleConstructionExpression(union, unionCase);
                            WriteLine($"{access} {tupleType} {GetAccessorName(union, unionCase)} => {GetTagComparison(union, unionCase)} ? {tupleConstruction} : default!;");
                        }
                        break;

                    case CaseAccessorKind.Method:
                        if (unionCase.FactoryParameters.Count == 0)
                        {
                            if (unionCase.Type != null && unionCase.Type.IsSingleton)
                            {
                                WriteLine($"{access} {unionCase.Type.Name} {GetAccessorName(union, unionCase)}() => {GetTagComparison(union, unionCase)} ? {GetCaseValueConstructionExpression(union, unionCase)} : default!;");
                            }
                            else
                            {
                                WriteLine($"{access} bool {GetAccessorName(union, unionCase)}() => {GetTagComparison(union, unionCase)};");
                            }
                        }
                        else if (unionCase.FactoryParameters.Count == 1)
                        {
                            var param = unionCase.FactoryParameters[0];
                            WriteLine($"{access} {param.Type.Name} {GetAccessorName(union, unionCase)}() => {GetTagComparison(union, unionCase)} ? {GetCaseValueConstructionExpression(union, unionCase)} : default!;");
                        }
                        else if (unionCase.FactoryParameters.Count > 1)
                        {
                            var tupleType = GetCaseTupleTypeExpression(unionCase);
                            var tupleConstruction = GetCaseTupleConstructionExpression(union, unionCase);
                            WriteLine($"{access} {tupleType} {GetAccessorName(union, unionCase)}() => {GetTagComparison(union, unionCase)} ? {tupleConstruction} : default!;");
                        }
                        break;
                }
            }
        }

        private void WriteCaseTuples(UnionLayout union)
        {
            WriteLineSeparatedBlocks(() =>
            {
                foreach (var unionCase in union.Cases)
                {
                    if (unionCase.FactoryParameters.Count > 1 && !CanUseTuple(unionCase.FactoryParameters))
                    {
                        WriteBlock(() =>
                        {
                            WriteLine($"{unionCase.Accessibility} ref struct {unionCase.Name}Tuple");
                            WriteBraceNested(() =>
                            {
                                foreach (var fp in unionCase.FactoryParameters)
                                {
                                    WriteLine($"{unionCase.Accessibility} {fp.Type.Name} {fp.Name};");
                                }

                                WriteLine();
                                var parameters = string.Join(", ", unionCase.FactoryParameters.Select(p => GetParameter(p)));
                                WriteLine($"{unionCase.Accessibility} {unionCase.Name}Tuple({parameters})");
                                WriteBraceNested(() =>
                                {
                                    foreach (var fp in unionCase.FactoryParameters)
                                    {
                                        WriteLine($"this.{fp.Name} = {fp.Name};");
                                    }
                                });
                            });
                        });
                    }
                }
            });
        }

        private static string GetParameter(CaseValueLayout caseValue)
        {
            return $"{caseValue.Type.Name} {caseValue.Name}";
        }

        private void WriteTryCreate(UnionLayout union)
        {
            if (union.Kind != UnionKind.TypeUnion)
                return;

            if (!CanUseGenericTypeParameters(union))
                return;

            WriteLine($"public static bool TryCreate<TCreate>(TCreate value, out {union.TypeName} union)");
            WriteBraceNested(() =>
            {
                WriteLine("switch (value)");
                WriteBraceNested(() =>
                {
                    foreach (var unionCase in union.Cases)
                    {
                        if (unionCase.Type != null)
                        {
                            WriteLine($"case {unionCase.Type.Name} v: union = {GetFactoryCallExpression(union, unionCase, "v")}; return true;");
                        }
                    }
                });

                if (union.Options.UseToolkit)
                {
                    WriteLine("return TypeUnion.TryCreateFromUnion(value, out union);");
                }
                else
                {
                    WriteLine("union = default!;");
                    WriteLine("return false;");
                }
            });
        }

        private void WriteTryGet(UnionLayout union)
        {
            if (union.Kind != UnionKind.TypeUnion)
                return;

            if (!CanUseGenericTypeParameters(union))
                return;

            WriteLine($"public bool TryGet<TGet>([NotNullWhen(true)] out TGet value)");
            WriteBraceNested(() =>
            {
                WriteLine("switch (this.Kind)");
                WriteBraceNested(() =>
                {
                    foreach (var unionCase in union.Cases)
                    {
                        WriteLine($"case {GetTagValueExpression(union, unionCase)}:");
                        WriteNested(() =>
                        {
                            WriteLine($"if ({GetCaseValueAccessExpression(union, unionCase)} is TGet tv{unionCase.Name})");
                            WriteBraceNested(() =>
                            {
                                WriteLine($"value = tv{unionCase.Name};");
                                WriteLine("return true;");
                            });

                            if (union.Options.UseToolkit)
                            {
                                WriteLine($"return TypeUnion.TryCreate({GetCaseValueAccessExpression(union, unionCase)}, out value);");
                            }
                            else
                            {
                                WriteLine("break;");
                            }
                        });
                    }
                });

                WriteLine("value = default!;");
                WriteLine("return false;");
            });
        }

        private void WriteITypeUnionMembers(UnionLayout union)
        {
            if (union.Kind != UnionKind.TypeUnion)
                return;

            if (!union.Options.UseToolkit)
                return;

            if (!CanUseGenericTypeParameters(union))
                return;

            WriteLineSeparatedBlocks(() =>
            {
                WriteBlock(() =>
                {
                    WriteLine("public Type Type");
                    WriteBraceNested(() =>
                    {
                        WriteLine("get");
                        WriteBraceNested(() =>
                        {
                            WriteLine("switch (this.Kind)");
                            WriteBraceNested(() =>
                            {
                                foreach (var unionCase in union.Cases)
                                {
                                    WriteLine($"case {GetTagValueExpression(union, unionCase)}: return typeof({unionCase.Type!.Name});");
                                }
                            });

                            WriteLine("return typeof(object);");
                        });
                    });
                });

                WriteBlock(() =>
                {
                    var types = union.Cases.Select(c => c.Type?.Name).OfType<string>();
                    var typeList = string.Join(", ", types.Select(t => $"typeof({t})"));
                    WriteLine($"static IReadOnlyList<Type> IClosedTypeUnion<{union.TypeName}>.Types {{ get; }} =");
                    WriteLineNested($"new [] {{ {typeList} }};");
                });
            });
        }

        private void WriteEqualityMethods(UnionLayout union)
        {
            if (!union.Options.GenerateEquality)
                return;

            var isParameterLessTagsOnly = union.Cases.All(c => c.FactoryParameters.Count == 0);

            WriteLineSeparatedBlocks(() =>
            {
                // IEquatable<Union>.Equals
                WriteBlock(() =>
                {
                    WriteLine($"public bool Equals({union.TypeName} other)");
                    WriteBraceNested(() =>
                    {
                        if (isParameterLessTagsOnly)
                        {
                            WriteLine($"return this.{GetTagPropertyName(union)} == other.{GetTagPropertyName(union)};");
                        }
                        else
                        {
                            WriteLine($"if (this.{GetTagPropertyName(union)} != other.{GetTagPropertyName(union)}) return false;");
                            WriteLine();
                            WriteLine($"switch (this.{GetTagPropertyName(union)})");
                            WriteBraceNested(() =>
                            {
                                foreach (var unionCase in union.Cases)
                                {
                                    WriteLine($"case {GetTagValueExpression(union, unionCase)}:");
                                    if (unionCase.FactoryParameters.Count == 0)
                                    {
                                        WriteLineNested($"return true;");
                                    }
                                    else
                                    {
                                        if (unionCase.FactoryParameters.Count == 1
                                            && IsPossibleReference(unionCase.FactoryParameters[0].Type.Kind))
                                        {
                                            WriteLineNested($"return object.Equals({GetCaseValueAccessExpression(union, unionCase)}, {GetCaseValueAccessExpression(union, unionCase, "other")});");
                                        }
                                        else
                                        {
                                            WriteLineNested($"return {GetCaseValueAccessExpression(union, unionCase)}.Equals({GetCaseValueAccessExpression(union, unionCase, "other")});");
                                        }
                                    }
                                }

                                WriteLine("default:");
                                WriteLineNested("return false;");
                            });
                        }
                    });
                });

                // object.Equals
                WriteBlock(() =>
                {
                    WriteLine("public override bool Equals(object? other)");
                    WriteBraceNested(() =>
                    {
                        if (union.Kind == UnionKind.TypeUnion)
                        {
                            // try to convert to same type and then use IEquatable<TUnion>.Equals
                            WriteLine($"return TryCreate(other, out var union) && this.Equals(union);");
                        }
                        else
                        {
                            // defer to IEquatable<TUnion>.Equals
                            WriteLine($"return other is {union.TypeName} union && this.Equals(union);");
                        }
                    });
                });

                // object.GetHashCode()
                WriteBlock(() =>
                {
                    WriteLine("public override int GetHashCode()");
                    WriteBraceNested(() =>
                    {
                        if (isParameterLessTagsOnly)
                        {
                            WriteLine($"return (int)this.{GetTagPropertyName(union)};");
                        }
                        else
                        {
                            WriteLine($"switch (this.{GetTagPropertyName(union)})");
                            WriteBraceNested(() =>
                            {
                                foreach (var unionCase in union.Cases)
                                {
                                    WriteLine($"case {GetTagValueExpression(union, unionCase)}:");

                                    if (unionCase.FactoryParameters.Count == 0)
                                    {
                                        WriteLineNested($"return (int)this.{GetTagPropertyName(union)};");
                                    }
                                    else if (unionCase.FactoryParameters.Count == 1 && IsPossibleReference(unionCase.FactoryParameters[0].Type.Kind))
                                    {
                                        WriteLineNested($"return {GetCaseValueAccessExpression(union, unionCase)}?.GetHashCode() ?? 0;");
                                    }
                                    else
                                    {
                                        WriteLineNested($"return {GetCaseValueAccessExpression(union, unionCase)}.GetHashCode();");
                                    }
                                }

                                WriteLine("default:");
                                WriteLineNested("return 0;");
                            });
                        }
                    });
                });

                // equality operators
                WriteBlock(() =>
                {
                    WriteLine($"public static bool operator == ({union.TypeName} left, {union.TypeName} right) => left.Equals(right);");
                    WriteLine($"public static bool operator != ({union.TypeName} left, {union.TypeName} right) => !left.Equals(right);");
                });
            });
        }

        private void WriteToString(UnionLayout union)
        {
            if (!union.Options.GenerateToString)
                return;

            var isParameterLessTagsOnly = union.Cases.All(c => c.FactoryParameters.Count == 0);

            WriteLine("public override string ToString()");
            WriteBraceNested(() =>
            {
                if (isParameterLessTagsOnly)
                {
                    WriteLine($"return this.{GetTagPropertyName(union)}.ToString();");
                }
                else
                {
                    WriteLine($"switch (this.{GetTagPropertyName(union)})");
                    WriteBraceNested(() =>
                    {
                        foreach (var unionCase in union.Cases)
                        {
                            WriteLine($"case {GetTagValueExpression(union, unionCase)}:");
                            if (union.Kind == UnionKind.TagUnion)
                            {
                                if (unionCase.FactoryParameters.Count == 0)
                                {
                                    WriteLineNested($"return \"{unionCase.Name}\";");
                                }
                                else if (unionCase.FactoryParameters.Count == 1)
                                {
                                    WriteLineNested($"return $\"{unionCase.Name}({{{GetCaseValueAccessExpression(union, unionCase)}}})\";");
                                }
                                else
                                {
                                    WriteNested(() =>
                                    {
                                        WriteLine($"var v_{unionCase.Name} = {GetCaseValueAccessExpression(union, unionCase)};");
                                        var props = string.Join(", ", unionCase.FactoryParameters.Select(p => $"{p.Name}: {{v_{unionCase.Name}.{p.Name}}}"));
                                        WriteLine($$"""return $"{{unionCase.Name}}({{props}})";""");
                                    });
                                }
                            }
                            else
                            {
                                if (unionCase.Type != null && IsPossibleReference(unionCase.Type.Kind))
                                {
                                    WriteLineNested($"return {GetCaseValueAccessExpression(union, unionCase)}?.ToString() ?? \"\";");
                                }
                                else
                                {
                                    WriteLineNested($"return {GetCaseValueAccessExpression(union, unionCase)}.ToString();");
                                }
                            }
                        }

                        WriteLine("default:");
                        WriteLineNested("return \"\";");
                    });
                }
            });
        }

        private static bool IsSingleValueAccessor(UnionCaseLayout unionCase) =>
            unionCase.AccessorKind == CaseAccessorKind.Property
            || unionCase.AccessorKind == CaseAccessorKind.Method;

        private void WriteMatchMethods(UnionLayout union)
        {
            if (!union.Options.GenerateMatch)
                return;

            var accessibility = union.Cases.All(c => c.Accessibility == "public")
                ? "public"
                : "internal";

            var hasDefault = union.Cases.Any(c => c.TagValue == 0);

            WriteLineSeparatedBlocks(() =>
            {
                // any required custom delegates
                WriteBlock(() =>
                {
                    foreach (var uc in union.Cases)
                    {
                        if (NeedsCustomDelegates(union, uc))
                        {
                            WriteLine(GetCustomActionTypeDeclaration(union, uc, accessibility));
                            WriteLine(GetCustomSelectorDeclaration(union, uc, accessibility));
                        }
                    }
                });

                WriteBlock(() =>
                {
                    var parameters = union.Cases
                        .Select(c => $"{GetActionTypeName(union, c)} when{c.Name}")
                        .ToList();

                    if (!hasDefault)
                        parameters.Add("Action? undefined = null");

                    var parameterList = string.Join(", ", parameters);

                    WriteLine($"{accessibility} void Match({parameterList})");
                    WriteBraceNested(() =>
                    {
                        WriteLine($"switch ({GetTagPropertyName(union)})");
                        WriteBraceNested(() =>
                        {
                            foreach (var unionCase in union.Cases)
                            {
                                switch (unionCase.AccessorKind)
                                {
                                    case CaseAccessorKind.Property:
                                    case CaseAccessorKind.Method:
                                        if (GetCaseValueTypeExpression(union, unionCase) != null)
                                        {
                                            WriteLine($"case {GetTagValueExpression(union, unionCase)} : when{unionCase.Name}({GetCaseValueAccessExpression(union, unionCase)}); break;");
                                        }
                                        else
                                        {
                                            WriteLine($"case {GetTagValueExpression(union, unionCase)} : when{unionCase.Name}(); break;");
                                        }
                                        break;
                                }
                            }

                            if (hasDefault)
                            {
                                WriteLine("default: throw new InvalidOperationException(\"Invalid union state\");");
                            }
                            else
                            {
                                WriteLine("default: if (undefined != null) undefined(); else throw new InvalidOperationException(\"Undefined union state.\"); break;");
                            }
                        });
                    });
                });

                WriteBlock(() =>
                {
                    var parameters = union.Cases
                        .Select(c => $"{GetSelectorTypeName(union, c, "TResult")} when{c.Name}")
                        .ToList();

                    if (!hasDefault)
                        parameters.Add("Func<TResult>? undefined = null");

                    var parameterList = string.Join(", ", parameters);

                    WriteLine($"{accessibility} TResult Select<TResult>({parameterList})");
                    WriteBraceNested(() =>
                    {
                        WriteLine($"switch ({GetTagPropertyName(union)})");
                        WriteBraceNested(() =>
                        {
                            foreach (var unionCase in union.Cases)
                            {
                                switch (unionCase.AccessorKind)
                                {
                                    case CaseAccessorKind.Property:
                                    case CaseAccessorKind.Method:
                                        if (GetCaseValueTypeExpression(union, unionCase) != null)
                                        {
                                            WriteLine($"case {GetTagValueExpression(union, unionCase)}: return when{unionCase.Name}({GetCaseValueAccessExpression(union, unionCase)});");
                                        }
                                        else
                                        {
                                            WriteLine($"case {GetTagValueExpression(union, unionCase)}: return when{unionCase.Name}();");
                                        }
                                        break;
                                }
                            }

                            if (hasDefault)
                            {
                                WriteLine("default: throw new InvalidOperationException(\"Invalid union state\");");
                            }
                            else
                            {
                                WriteLine("default: return undefined != null ? undefined() : throw new InvalidOperationException(\"Undefined union state.\");");
                            }
                        });
                    });
                });
            });
        }

        private static bool NeedsCustomDelegates(UnionLayout union, UnionCaseLayout unionCase)
        {
            if (unionCase.Type != null)
                return !CanBeGenericArgument(unionCase.Type);
            return unionCase.FactoryParameters.Any(p => !CanBeGenericArgument(p.Type));
        }

        private static string GetActionTypeName(UnionLayout union, UnionCaseLayout unionCase)
        {
            if (NeedsCustomDelegates(union, unionCase))
            {
                return $"ActionOf{unionCase.Name}";
            }
            var type = GetCaseValueTypeExpression(union, unionCase);
            if (type != null)
            {
                return $"Action<{type}>";
            }
            else
            {
                return "Action";
            }
        }

        private static string GetCustomActionTypeDeclaration(UnionLayout union, UnionCaseLayout unionCase, string accessibility)
        {
            return $"{accessibility} delegate void ActionOf{unionCase.Name}({GetCaseValueTypeExpression(union, unionCase)} value);";
        }

        private static string GetSelectorTypeName(UnionLayout union, UnionCaseLayout unionCase, string resultType)
        {
            if (NeedsCustomDelegates(union, unionCase))
            {
                return $"FuncOf{unionCase.Name}<{resultType}>";
            }
            var type = GetCaseValueTypeExpression(union, unionCase);
            if (type != null)
            {
                return $"Func<{type}, {resultType}>";
            }
            else
            {
                return $"Func<{resultType}>";
            }
        }

        private static string GetCustomSelectorDeclaration(UnionLayout union, UnionCaseLayout unionCase, string accessibility)
        {
            return $"{accessibility} delegate TResult FuncOf{unionCase.Name}<TResult>({GetCaseValueTypeExpression(union, unionCase)} value);";
        }

        #endregion

        #region helpers

        /// <summary>
        /// Constructs the union type for a given case from factory arguments.
        /// </summary>
        private string GetUnionCaseConstructorExpression(UnionLayout union, UnionCaseLayout unionCase)
        {
            var args = new List<string>();
            args.Add($"{GetTagArgumentName(union)}: {GetTagValueExpression(union, unionCase)}");

            if (union.OverlappedDataField != null)
            {
                args.Add($"{union.OverlappedDataField.ConstructorArg}: {GetOverlappedConstructionExpression(union, unionCase)}");
            }

            foreach (var field in union.NonOverlappedFields)
            {
                if (field.ConstructorArg != null)
                {
                    if (unionCase.TryGetParameter(field, out var param))
                    {
                        args.Add($"{field.ConstructorArg}: {param.PathFromFactoryArg}");
                    }
                    else
                    {
                        args.Add($"{field.ConstructorArg}: default!");
                    }
                }
            }

            var argList = string.Join(", ", args);
            return $"new {union.TypeName}({argList})";

            string GetOverlappedConstructionExpression(UnionLayout union, UnionCaseLayout unionCase)
            {
                if (union.OverlappedDataField == null)
                    throw new InvalidOperationException("No overlapped data.");

                if (unionCase.OverlappedCaseField != null)
                {
                    var caseConstruction = GetOverlappedCaseConstructionExpression();
                    return $"new {union.OverlappedDataField.TypeName} {{ {unionCase.OverlappedCaseField.Name} = {caseConstruction} }}";
                }
                else
                {
                    return "default!";
                }

                string GetOverlappedCaseConstructionExpression()
                {
                    if (unionCase.OverlappedCaseDataFields.Count == 0)
                    {
                        // there is only one value, so no struct is defined
                        // find the value that references the overlapped case field
                        var value = FindFirst(unionCase.FactoryParameters, p => p.Field == unionCase.OverlappedCaseField);
                        if (value != null && value.PathFromFactoryArg != null)
                        {
                            return value.PathFromFactoryArg;
                        }
                        else
                        {
                            throw new InvalidOperationException("No overlapped case field value found.");
                        }
                    }
                    else
                    {
                        var assignments = new List<string>();
                        GetOverlappedCaseDataFieldAssignments(unionCase.FactoryParameters, assignments);
                        var assignmentList = string.Join(", ", assignments);
                        return $"new {unionCase.OverlappedCaseField!.TypeName} {{ {assignmentList} }}";

                        void GetOverlappedCaseDataFieldAssignments(IEnumerable<CaseValueLayout> values, List<string> assigments)
                        {
                            foreach (var value in values)
                            {
                                if (value.Field != null
                                    && value.DataKind == DataKind.Overlappable
                                    && value.PathFromFactoryArg != null)
                                {
                                    assigments.Add($"{value.Field.Name} = {value.PathFromFactoryArg}");
                                }
                                else if (value.DataKind == DataKind.Decomposable)
                                {
                                    GetOverlappedCaseDataFieldAssignments(value.Members, assigments);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the text of an expression that constructs the case value from fields.
        /// </summary>
        private static string GetCaseValueConstructionExpression(UnionLayout union, UnionCaseLayout unionCase)
        {
            if (unionCase.FactoryParameters.Count == 1)
            {
                // treat as single parameter value
                return GetCaseValueConstructionExpression(union, unionCase.FactoryParameters[0]);
            }
            else if (unionCase.FactoryParameters.Count > 1)
            {
                // expected to be able to convert to tuple
                if (unionCase.AccessorKind == CaseAccessorKind.Property
                    || unionCase.AccessorKind == CaseAccessorKind.Method)
                {
                    // treat as tuple of parameter values
                    return GetCaseTupleConstructionExpression(union, unionCase);
                }
                else
                {
                    // cannot construct this value..
                    return "";
                }
            }
            else if (unionCase.FactoryParameters.Count == 0
                && unionCase.Type != null && unionCase.Type.SingletonAccessor != null)
            {
                return $"{unionCase.Type.Name}.{unionCase.Type.SingletonAccessor}";
            }
            else
            {
                // no parameters, no type, no singleton
                return "default!";
            }
        }

        /// <summary>
        /// Gets the text of an expression that constructs the case value from fields.
        /// </summary>
        private static string GetCaseValueConstructionExpression(UnionLayout union, CaseValueLayout value)
        {
            if (value.DataKind == DataKind.Decomposable)
            {
                switch (value.Type.Kind)
                {
                    case TypeKind.DecomposableForeignRecordStruct:
                    case TypeKind.DecomposableLocalRecordStruct:
                    case TypeKind.OverlappableLocalRecordStruct:
                    case TypeKind.OverlappableForeignRecordStruct:
                    case TypeKind.OverlappableDecomposableLocalRecordStruct:
                    case TypeKind.OverlappableDecomposableForeignRecordStruct:
                        return GetRecordConstructionExpression(union, value);

                    case TypeKind.NonOverlappableTuple:
                    case TypeKind.OverlappableTuple:
                        return GetTupleConstructionExpression(union, value.Members);
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (value.Field != null)
            {
                var path = GetPathToData(union, value.Field);

                // pull the entire reference type case instance from a single value field
                if (value.DataKind == DataKind.ReferenceSharable
                    && value.Field.TypeName != value.Type.Name)
                {
                    return $"({value.Type.Name}){path}";
                }
                else
                {
                    return path;
                }
            }
            else
            {
                throw new InvalidOperationException($"Parameter {value.Name} has no field.");
            }
        }

        private static string GetRecordConstructionExpression(UnionLayout union, CaseValueLayout value)
        {
            var members = value.Members.Select(v => GetCaseValueConstructionExpression(union, v));
            return $"new {value.Type.Name}(" + string.Join(", ", members) + ")";
        }

        /// <summary>
        /// True if the type union can use generic type parameters to refer to case values.
        /// </summary>
        private static bool CanUseGenericTypeParameters(UnionLayout union)
        {
            return union.Kind == UnionKind.TypeUnion 
                && union.Cases.All(c => c.Type != null && CanBeGenericArgument(c.Type));
        }

        /// <summary>
        /// True if the type can be used as a generic type argument.
        /// </summary>
        private static bool CanBeGenericArgument(UnionValueType type)
        {
            return type.Kind != TypeKind.RefStruct;
        }

        private static bool CanUseTuple(IReadOnlyList<CaseValueLayout> values)
        {
            // must have at least two values, and those values can be represented with generic type arguments.
            return values.Count > 1 && values.All(v => CanBeGenericArgument(v.Type));
        }

        private static string GetCaseTupleTypeExpression(UnionCaseLayout unionCase)
        {
            if (CanUseTuple(unionCase.FactoryParameters))
            {
                return "(" + string.Join(", ", unionCase.FactoryParameters.Select(cv => $"{cv.Type.Name} {cv.Name}")) + ")";
            }
            else
            {
                return $"{unionCase.Name}Tuple";
            }
        }

        private static string GetCaseTupleConstructionExpression(UnionLayout union, UnionCaseLayout unionCase)
        {
            if (CanUseTuple(unionCase.FactoryParameters))
            {
                return GetTupleConstructionExpression(union, unionCase.FactoryParameters);
            }
            else
            {
                var values = unionCase.FactoryParameters.Select(p => GetCaseValueConstructionExpression(union, p));
                return $"new {unionCase.Name}Tuple({string.Join(", ", values)})";
            }
        }

        private static string GetTupleConstructionExpression(UnionLayout union, IReadOnlyList<CaseValueLayout> caseValues)
        {
            var values = caseValues.Select(p => GetCaseValueConstructionExpression(union, p));
            return "(" + string.Join(", ", values) + ")";
        }

        /// <summary>
        /// Gets an expression that accesses the case values from the accessor.
        /// </summary>
        private static string GetCaseValueAccessExpression(UnionLayout union, UnionCaseLayout unionCase, string prefix = "this")
        {
            switch (unionCase.AccessorKind)
            {
                case CaseAccessorKind.Property:
                    return $"{prefix}.{GetAccessorName(union, unionCase)}";
                case CaseAccessorKind.Method:
                    return $"{prefix}.{GetAccessorName(union, unionCase)}()";
                default:
                    return GetCaseValueConstructionExpression(union, unionCase);
            }
        }

        /// <summary>
        /// Gets the type that the case value accessor returns.
        /// </summary>
        private static string? GetCaseValueTypeExpression(UnionLayout union, UnionCaseLayout unionCase)
        {
            if (unionCase.FactoryParameters.Count == 1)
            {
                return unionCase.FactoryParameters[0].Type.Name;
            }
            else if (unionCase.FactoryParameters.Count > 1)
            {
                return GetCaseTupleTypeExpression(unionCase);
            }
            else if (unionCase.FactoryParameters.Count == 0
                && unionCase.Type != null && unionCase.Type.IsSingleton)
            {
                return unionCase.Type.Name;
            }
            else
            {
                // there is no type.
                return null;
            }
        }

        private static string GetFactoryCallExpression(UnionLayout union, UnionCaseLayout unionCase, string args)
        {
            switch (unionCase.FactoryKind)
            {
                case CaseFactoryKind.Property:
                    return $"{union.TypeName}.{GetFactoryName(union, unionCase)}";

                case CaseFactoryKind.Method:
                    if (unionCase.FactoryParameters.Count > 0)
                    {
                        return $"{union.TypeName}.{GetFactoryName(union, unionCase)}({args})";
                    }
                    else
                    {
                        return $"{union.TypeName}.{GetFactoryName(union, unionCase)}()";
                    }
                default:
                    return "default";
            }
        }

        private static string GetTagTypeName(UnionLayout union)
        {
            return union.Options.TagTypeName;
        }

        private static string GetCaseTagName(UnionCaseLayout unionCase)
        {
            return unionCase.Name;
        }

        private static string GetTagPropertyName(UnionLayout union)
        {
            return union.Options.TagPropertyName;
        }

        private static string GetTagArgumentName(UnionLayout union)
        {
            return LowerName(GetTagPropertyName(union));
        }

        private static string GetTagValueExpression(UnionLayout union, UnionCaseLayout unionCase)
        {
            return $"{union.TypeName}.{GetTagTypeName(union)}.{GetCaseTagName(unionCase)}";
        }

        private static string GetTagComparison(UnionLayout union, UnionCaseLayout unionCase)
        {
            return $"this.{GetTagPropertyName(union)} == {GetTagValueExpression(union, unionCase)}";
        }

        private static string GetFactoryName(UnionLayout union, UnionCaseLayout unionCase)
        {
            if (unionCase.Case.FactoryName != null)
                return unionCase.Case.FactoryName;

            if (union.Kind == UnionKind.TagUnion)
                return unionCase.Name;

            if (union.Kind == UnionKind.TypeUnion 
                && unionCase.Case.FactoryKind == CaseFactoryKind.Property 
                && unionCase.Type != null
                && unionCase.Type.IsSingleton)
                return unionCase.Name;

            return "Create";
        }

        private static string GetAccessorName(UnionLayout union, UnionCaseLayout unionCase)
        {
            if (unionCase.Case.AccessorName != null)
                return unionCase.Case.AccessorName;

            switch (unionCase.AccessorKind)
            {
                case CaseAccessorKind.Property:
                case CaseAccessorKind.Method:
                    if (unionCase.FactoryParameters.Count == 0)
                    {
                        if (unionCase.Type != null && unionCase.Type.IsSingleton)
                        {
                            return unionCase.Name + "Value";
                        }
                        else
                        {
                            return "Is" + unionCase.Name;
                        }
                    }
                    else if (unionCase.FactoryParameters.Count > 1)
                    {
                        return unionCase.Name + "Values";
                    }
                    break;
            }

            return unionCase.Name + "Value";
        }


        private static IEnumerable<CaseValueLayout> FindAll(IEnumerable<CaseValueLayout> values, Func<CaseValueLayout, bool> predicate)
        {
            var list = new List<CaseValueLayout>();
            Find(values, list);
            return list;

            void Find(IEnumerable<CaseValueLayout> values, List<CaseValueLayout> list)
            {
                foreach (var value in values)
                {
                    if (predicate(value))
                        list.Add(value);

                    Find(value.Members, list);
                }
            }
        }

        private static CaseValueLayout? FindFirst(IEnumerable<CaseValueLayout> values, Func<CaseValueLayout, bool> predicate)
        {
            foreach (var value in values)
            {
                if (predicate(value))
                    return value;
                var found = FindFirst(value.Members, predicate);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Gets the path to the data field.
        /// </summary>
        private static string GetPathToData(UnionLayout union, DataField field)
        {
            if (field == union.OverlappedDataField
                || union.NonOverlappedFields.Contains(field))
            {
                return field.Name;
            }

            if (union.OverlappedDataField != null)
            {
                foreach (var unionCase in union.Cases)
                {
                    if (unionCase.OverlappedCaseField == field)
                    {
                        return CombinePath(union.OverlappedDataField.Name, field.Name);
                    }

                    if (unionCase.OverlappedCaseField != null
                        && unionCase.OverlappedCaseDataFields.Contains(field))
                    {
                        return CombinePath(GetPathToData(union, unionCase.OverlappedCaseField), field.Name);
                    }
                }
            }

            throw new InvalidOperationException("Data field not in model");
        }

        private static string CombinePath(params string[] parts)
        {
            return string.Join(".", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        private static string CombineName(params string[] parts)
        {
            return string.Join("_", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        #endregion

    }

#if !T4
}
#endif
// #>