using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CATypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace UnionTypes.Toolkit.Generators
{
    [Generator(LanguageNames.CSharp)]
    public class CustomUnionSourceGenerator: IIncrementalGenerator
    {
        public static readonly string TagUnionAttributeName = "TagUnionAttribute";
        public static readonly string TypeUnionAttributeName = "TypeUnionAttribute";
        public static readonly string CaseAttributeName = "CaseAttribute";
        public static readonly string TagUnionAnnotation = "@TagUnion";
        public static readonly string TypeUnionAnnotation = "@TypeUnion";

        public static readonly string GetInfoStepName = "GetInfo";
        public static readonly string GenerateStepName = "Generate";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider =
                context.SyntaxProvider.CreateSyntaxProvider(IsGenerationCandiate, GetGenerationInfo)
                .WithTrackingName(GetInfoStepName)
                .Select(Generate)
                .WithTrackingName(GenerateStepName)
                .Where(info => info != null);

            context.RegisterSourceOutput(provider, GenerateOutput);
        }

        public bool IsGenerationCandiate(SyntaxNode node, CancellationToken ct)
        {
            // must be partial struct with [Union] attribute
            if (node is StructDeclarationSyntax decl
                && decl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the info that drives the code generation for the union type.
        /// </summary>
        public GenerationInfo? GetGenerationInfo(GeneratorSyntaxContext context, CancellationToken ct)
        {
            var decl = (StructDeclarationSyntax)context.Node;
            
            var symbol = context.SemanticModel.GetDeclaredSymbol(decl, ct);
            if (symbol != null
                && TryGetGenerationInfo(symbol, out var union))
            {
                return union;
            }

            return null!;
        }

        private GenerateResult? Generate(GenerationInfo? info, CancellationToken ct)
        {
            if (info != null)
            {
                if (info.Diagnostics.Count > 0)
                {
                    return new GenerateResult("", "", info.Diagnostics);
                }
                else
                {
                    var generator = new CustomUnionGenerator();
                    var text = generator.Generate(info.Union);
                    var name = (string.IsNullOrEmpty(info.Namespace) ? info.Union.SimpleName : $"{info.Namespace}_{info.Union.SimpleName}").Replace('.', '_');
                    var fileName = $"{name}_UnionImplementation.cs";
                    return new GenerateResult(text, fileName, info.Diagnostics);
                }
            }

            return null;
        }

        private void GenerateOutput(SourceProductionContext context, GenerateResult? resultx)
        {
            if (resultx is GenerateResult result)
            {
                if (result.Diagnostics.Count > 0)
                {
                    foreach (var dx in result.Diagnostics)
                    {
                        context.ReportDiagnostic(dx);
                    }
                }
                else
                {
                    context.AddSource(result.FileName, result.Text);
                }
            }
        }

        public class GenerationInfo : IEquatable<GenerationInfo>
        {
            public string Namespace { get; }
            public IReadOnlyList<string> Usings { get; }
            public UnionInfo Union { get; }
            public IReadOnlyList<Diagnostic> Diagnostics { get; }

            public GenerationInfo(
                string @namespace, 
                IReadOnlyList<string> usings, 
                UnionInfo union,
                IReadOnlyList<Diagnostic> diagnostics
                )
            {
                this.Namespace = @namespace;
                this.Usings = usings;
                this.Union = union;
                this.Diagnostics = diagnostics;
            }

            public bool Equals(GenerationInfo generationInfo)
            {
                var isEqual = Namespace == generationInfo.Namespace
                    && Usings.SequenceEqual(generationInfo.Usings)
                    && Union.Equals(generationInfo.Union)
                    && Diagnostics.SequenceEqual(generationInfo.Diagnostics);
                return isEqual;
            }

            public override bool Equals(object obj) =>
                obj is GenerationInfo info && Equals(info);

            public override int GetHashCode() =>
                this.Union.GetHashCode();
        }

        private class GenerateResult
        {
            public string Text { get; }
            public string FileName { get; }
            public IReadOnlyList<Diagnostic> Diagnostics { get; }

            public GenerateResult(
                string text, 
                string fileName,
                IReadOnlyList<Diagnostic> diagnostics)
            {
                Text = text;
                FileName = fileName;
                Diagnostics = diagnostics;
            }
        }

        /// <summary>
        /// Gets the info that drives the code generation for the union type.
        /// </summary>
        private bool TryGetGenerationInfo(INamedTypeSymbol unionType, out GenerationInfo info)
        {
            var cases = new List<CaseDesc>();
            var diagnostics = new List<Diagnostic>();

            string namespaceName = null!;
            if (unionType.ContainingNamespace != null)
            {
                namespaceName = GetNamespaceName(unionType.ContainingNamespace);
            }

            var usingDirectives = GetDeclaredUsings(unionType);
            var usings = usingDirectives.Select(uz => uz.ToString()).ToArray();

            var modifiers = GetModifiers(unionType);
            var accessibility = GetMemberAccessibilityForType(unionType);

            var style = GetLayoutStyleFromComments(unionType, LayoutStyle.Tagged);

            // get all cases declared for union type
            //GetTypeCasesFromNestedTypes(unionType, cases, diagnostics);
            GetTypeCasesFromPrivateCaseMethod(unionType, cases, diagnostics);

            if (cases.Count > 0)
            {
                var fullName = GetTypeFullName(unionType);

                var union = new UnionInfo(
                    fullName,
                    cases,
                    style,
                    accessibility
                    );

                info = new GenerationInfo(namespaceName, usings, union, diagnostics);
                return true;
            }

            info = default!;
            return false;
        }

        /// <summary>
        /// Gets the using directives from the file that contains the type declaration.
        /// </summary>
        private IReadOnlyList<UsingDirectiveSyntax> GetDeclaredUsings(INamedTypeSymbol type)
        {
            if (type.Locations.FirstOrDefault(loc => loc.IsInSource) is Location sourceLocation
                && sourceLocation.SourceTree is SyntaxTree sourceTree
                && sourceTree.GetRoot() is SyntaxNode root)
            {
                return root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Where(u => u.GlobalKeyword == default)
                    .ToList();
            }

            return Array.Empty<UsingDirectiveSyntax>();
        }

        /// <summary>
        /// Gets <see cref="LayoutStyle"/> from comments on the declared union type.
        /// </summary>
        private LayoutStyle GetLayoutStyleFromComments(ISymbol symbol, LayoutStyle defaultStyle)
        {
            if (TryGetCommentProperty(symbol, "Layout", out var value))
            {
                if (Enum.TryParse(value, ignoreCase: true, out LayoutStyle style))
                {
                    return style;
                }
            }

            return defaultStyle;
        }

        private void GetTypeCasesFromPrivateCaseMethod(
            INamedTypeSymbol unionType, 
            List<CaseDesc> cases,
            List<Diagnostic> diagnostics)
        {
            // find all "Case" methods that are private static void and have at least one parameter
            // use these to determine the set of case from the parameter types.
            var caseMethod = 
                unionType.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == "Cases"
                         && m.DeclaredAccessibility == Accessibility.Private
                         && m.TypeParameters.Length == 0
                         && m.ReturnType != null && m.ReturnType.SpecialType == SpecialType.System_Void
                         && m.Parameters.Length > 0);

            var caseTypes = new List<ITypeSymbol>();
            var caseParams = new List<IParameterSymbol>();

            if (caseMethod != null)
            {
                foreach (var pm in caseMethod.Parameters)
                {
                    caseTypes.Add(pm.Type);
                    caseParams.Add(pm);
                }

                for (int i = 0; i < caseTypes.Count; i++)
                {
                    var caseType = caseTypes[i];
                    var declaringNode = caseParams[i].DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()!;
                    var caseDesc = GetCaseDesc(caseTypes, i, diagnostics, declaringNode);
                    cases.Add(caseDesc);
                }
            }
        }
     
        /// <summary>
        /// Builds a <see cref="CaseDesc"/> for the case type at the given index in the list of case types.
        /// </summary>
        private CaseDesc GetCaseDesc(IReadOnlyList<ITypeSymbol> caseTypes, int caseIndex, List<Diagnostic> diagnostics, SyntaxNode caseDeclaration)
        {
            var type = caseTypes[caseIndex];
            var nnType = type.GetNonNullableType();
            ReportUnsupportedCaseTypes(nnType, diagnostics, caseDeclaration);

            var typeName = GetTypeFullName(type);
            var kind = GetTypeDescKind(nnType);

            var storageKind = GetStorageKind(nnType, diagnostics, caseDeclaration, useOverride: true);

            var members = 
                (storageKind == StorageKind.Decompose 
                    || (storageKind == StorageKind.Overlap && IsTrustedDecomposableType(nnType)))
                    ? GetDecomposibleMembers(nnType, diagnostics, caseDeclaration)
                    : Array.Empty<MemberDesc>();

            var typeDesc = new TypeDesc(typeName, kind, storageKind, members);

            // determine which other cases are not truly disjoin from this one
            var nonDisjointCases = Enumerable.Range(0, caseTypes.Count)
                .Where(i => i != caseIndex && !AreDisjoint(type, caseTypes[i]))
                .ToList();

            var accessibility = GetMemberAccessibilityForType(type);

            return new CaseDesc(typeDesc, nonDisjointCases, accessibility);
        }

        /// <summary>
        /// Return true if two types are disjoint (can never contain the same value as the other.)
        /// </summary>
        private bool AreDisjoint(ITypeSymbol typeA, ITypeSymbol typeB)
        {
            if (typeA.IsNullable())
                typeA = typeA.GetNonNullableType();

            if (typeB.IsNullable())
                typeB = typeB.GetNonNullableType();

            // if they are exactly the same type, then they are certainly not disjoint from each other.
            if (SymbolEqualityComparer.Default.Equals(typeA, typeB))
                return false;

            if (typeA.TypeKind == TypeKind.Interface)
            {
                switch (typeB.TypeKind)
                {
                    case TypeKind.Interface:
                        return false;
                    default:
                        // check the reverse instead
                        return AreDisjoint(typeB, typeA);
                }
            }
            else if (typeA.TypeKind == TypeKind.TypeParameter)
            {
                // since type parameters are not statically known at compile time, they may contain any value, and thus not provably disjoint.
                // In the case of constrained type parameters, if one is reference constrained and the other is value type constrained,
                // the reference constrained one might be an interface, so it might contain the same value as B.
                return false;
            }
            else if (typeA.TypeKind == TypeKind.Class)
            {
                switch (typeB.TypeKind)
                {
                    case TypeKind.Class:
                        // If A and B are classes then they are disjoint if one is not a subtype of the other.
                        return !IsSubTypeOf(typeA, typeB) && !IsSubTypeOf(typeB, typeA);
                    case TypeKind.Interface:
                        // if B is an interface, then A is disjoint if it is known to not implement the interface
                        return typeA.IsSealed && !ImplementsOrExtendsInterface(typeA, typeB);
                    case TypeKind.TypeParameter:
                        // type parameters are never disjoint from other types
                        return false;
                    default:
                        return true;
                }
            }
            else if (typeA.TypeKind == TypeKind.Struct)
            {
                switch (typeB.TypeKind)
                {
                    case TypeKind.Interface:
                        // if B is an interface, then A is disjoint if it is known to not implement the interface
                        return !ImplementsOrExtendsInterface(typeA, typeB);
                    case TypeKind.TypeParameter:
                        return false;
                    default:
                        return true;
                }
            }
            else if (typeA.TypeKind == TypeKind.Array)
            {
                switch (typeB.TypeKind)
                {
                    case TypeKind.Array:
                        // arrays are disjoint if their element types are disjoint
                        return AreDisjoint(((IArrayTypeSymbol)typeA).ElementType, ((IArrayTypeSymbol)typeB).ElementType);
                    case TypeKind.Interface:
                        // if B is an interface, then A is disjoint if it is known to not implement the interface
                        return !ImplementsOrExtendsInterface(typeA, typeB);
                    case TypeKind.TypeParameter:
                        return false;
                    default:
                        return true;
                }
            }
            else if (typeA.TypeKind == TypeKind.Enum)
            {
                return true;
            }
            else if (typeA.TypeKind == TypeKind.Delegate)
            {
                return true;
            }

            // otherwise, assume they are disjoint.
            return true;
        }

        private static bool ImplementsOrExtendsInterface(ITypeSymbol type, ITypeSymbol interfaceType)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
            }
            else if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
            {
                return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
            }
            else
            {
                return false;
            }
        }

        private static bool IsSubTypeOf(ITypeSymbol typeA, ITypeSymbol typeB)
        {
            if (SymbolEqualityComparer.Default.Equals(typeA, typeB))
                return false;

            if (typeB.TypeKind == TypeKind.Class && typeB.IsSealed)
            {
                // if B is a sealed class, then A cannot be a subtype of B
                return false;
            }

            if (typeA.TypeKind == TypeKind.Class)
            {
                var baseType = typeA.BaseType;
                while (baseType != null)
                {
                    if (SymbolEqualityComparer.Default.Equals(baseType, typeB))
                        return true;
                    baseType = baseType.BaseType;
                }
            }
            else if (typeA.TypeKind == TypeKind.Struct)
            {
                // structs cannot be subtypes of other types
                return false;
            }
            else if (typeA.TypeKind == TypeKind.Interface)
            {
                return typeA.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, typeB));
            }

            return false;
        }

        private IReadOnlyList<MemberDesc> GetDecomposibleMembers(ITypeSymbol type, List<Diagnostic> diagnostics, SyntaxNode caseDeclaration)
        {
            if (type.IsValueType
                && type is INamedTypeSymbol nt)
            {
                if (type.IsTupleType)
                {
                    // for tuples all members are in the constructor
                    var constructor = nt.Constructors.FirstOrDefault(c => c.Parameters.Length > 0);
                    if (constructor != null)
                    {
                        return constructor.Parameters.Select(p => CreateCaseMember(GetTuplePropertyName(p), p.Type, isParameter: true, diagnostics, caseDeclaration)).ToArray();
                    }
                }
                else if (!IsPrimitiveStruct(type))
                {
                    var members = new List<MemberDesc>();

                    // break down record into members based on primary constructor
                    // For records, the names of the parameters are the same as the names of the properties.
                    var primaryConstructor = nt.GetRecordPrimaryConstructor();
                    if (primaryConstructor != null)
                    {
                        members.AddRange(primaryConstructor.Parameters.Select(p => CreateCaseMember(p.Name, p.Type, isParameter: true, diagnostics, caseDeclaration)));
                    }
                    else
                    {
                        var defaultConstructor = nt.Constructors.FirstOrDefault(c => c.Parameters.Length == 0);

                        // no default constructor, so look for
                        // find first non-default constructor that has a matching constructor
                        var matchingConstructorAndDeconstructors = nt.GetMembers().OfType<IMethodSymbol>()
                            .Where(m => 
                                m.MethodKind == MethodKind.Constructor 
                                && m.Parameters.Length > 0)
                            .Select(m =>
                                (Constructor: m, Deconstructor: nt.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(d => 
                                    d.Name == "Deconstruct" 
                                    && d.Parameters.Select(dp => dp.Type).SequenceEqual(m.Parameters.Select(mp => mp.Type), SymbolEqualityComparer.Default)
                                    )))
                            .Where(pair => pair.Deconstructor != null);

                        var bestMatchingPair = MaxBy(matchingConstructorAndDeconstructors, x => x.Constructor.Parameters.Length);
                        if (bestMatchingPair != default)
                        {
                            // we have a constructor/destructor pair
                            members.AddRange(bestMatchingPair.Constructor.Parameters.Select(p => CreateCaseMember(p.Name, p.Type, isParameter: true, diagnostics, caseDeclaration)));
                        }
                        else if (defaultConstructor == null && !type.IsValueType)
                        {
                            // if there is no default constructor, then we cannot decompose this type.
                            ReportNonDecomposableCase(type, diagnostics, caseDeclaration);
                            return Array.Empty<MemberDesc>();
                        }
                    }

                    // use public fields and properties as additional decomposable members
                    var fieldOrPropertyMembers = nt.GetMembers()
                        .Where(m => 
                            !m.IsStatic 
                            && m.DeclaredAccessibility == Accessibility.Public
                            && !members.Any(mem => mem.Name == m.Name) // exclude members already accounted for in primary constructor
                            && (m is IFieldSymbol
                                || (m is IPropertySymbol p && p.GetMethod != null && p.SetMethod != null)))
                        .ToArray();

                    members.AddRange(
                        fieldOrPropertyMembers.Select(m => 
                        CreateCaseMember(
                            m.Name, 
                            m is IFieldSymbol f ? f.Type : ((IPropertySymbol)m).Type, 
                            isParameter: false, 
                            diagnostics, 
                            caseDeclaration
                            )));

                    return members;
                }
            }

            return Array.Empty<MemberDesc>();
        }

        private static T? MaxBy<T, TKey>(IEnumerable<T> source, Func<T, TKey> selector)
            where TKey : IComparable<TKey>
        {
            T? max = default;
            TKey? maxKey = default;

            foreach (var item in source)
            {
                var key = selector(item);
                if (maxKey == null || key.CompareTo(maxKey) > 0)
                {
                    max = item;
                    maxKey = key;
                }
            }

            return max;
        }

       /// <summary>
        /// Gets a deconstructable member of a case type from a parameter symbol,
        /// typically from the constructor of the type (record)
        /// </summary>
        private MemberDesc CreateCaseMember(string name, ITypeSymbol type, bool isParameter, List<Diagnostic> diagnostics, SyntaxNode caseDeclaration)
        {
            var kind = GetTypeDescKind(type);
            var typeName = GetTypeFullName(type);
            var storageKind = GetStorageKind(type, diagnostics, caseDeclaration, useOverride: false);
            var nestedMembers = GetDecomposibleMembers(type, diagnostics, caseDeclaration);
            var typeDesc = new TypeDesc(typeName, kind, storageKind, nestedMembers);
            var memberDesc = new MemberDesc(name, typeDesc, isParameter);
            return memberDesc;
        }

        private static string GetTuplePropertyName(IParameterSymbol parameter)
        {
            if (parameter.Name.StartsWith("item"))
            {
                return "Item" + parameter.Name.Substring(4);
            }
            else
            {
                return parameter.Name;
            }
        }

        private static StorageKind GetStorageOverride(ITypeSymbol type, SyntaxNode caseDeclaration)
        {
            if (ContainsInTrivia(caseDeclaration, "box"))
            {
                return StorageKind.Box;               
            }
            else if (ContainsInTrivia(caseDeclaration, "decompose"))
            {
                return StorageKind.Decompose;
            }
            else if (ContainsInTrivia(caseDeclaration, "isolate"))
            {
                return StorageKind.Isolate;               
            }
            else if (ContainsInTrivia(caseDeclaration, "overlap"))
            {
                return StorageKind.Overlap;                
            }
            else
            {
                return StorageKind.None;           
            }
        }

        private static StorageCapable GetStorageCapable(ITypeSymbol type)
        {
            if (type.IsNullable())
                return GetStorageCapable(type.GetNonNullableType());

            switch (type.TypeKind)
            {
                case CATypeKind.Enum:
                    return StorageCapable.Boxable | StorageCapable.Overlappable;
                case CATypeKind.Struct:
                    if (IsPrimitiveStruct(type))
                    {
                        return StorageCapable.Boxable | StorageCapable.Overlappable;
                    }
                    else if (type.IsTupleType)
                    {
                        if (IsTrustedOverlappableType(type))
                        {
                            return StorageCapable.Boxable | StorageCapable.Decomposable | StorageCapable.Overlappable;
                        }
                        else
                        {
                            return StorageCapable.Boxable | StorageCapable.Decomposable;
                        }
                    }
                    else if (type.IsRefLikeType)
                    {
                        // cannot be boxed, decomposed or overlapped
                        return StorageCapable.None;
                    }
                    else
                    {
                        var isDecomposable = IsTrustedDecomposableType(type);
                        var isOverlappable = IsTrustedOverlappableType(type);
                        
                        return StorageCapable.Boxable
                            | (isOverlappable ? StorageCapable.Overlappable : StorageCapable.None)
                            | (isDecomposable ? StorageCapable.Decomposable : StorageCapable.None);
                    }
                case CATypeKind.Interface:
                case CATypeKind.Class:
                case CATypeKind.Array:
                case CATypeKind.Dynamic:
                case CATypeKind.Delegate:
                    return StorageCapable.Boxable;                
                case CATypeKind.TypeParameter:
                    return StorageCapable.Boxable;
                default:
                    return StorageCapable.None;
            }           
        }

        /// <summary>
        /// Determines the <see cref="StorageKind"/> to use for this type.
        /// Reports errors for invalid override annotations.
        /// </summary>
        private static StorageKind GetStorageKind(ITypeSymbol type, List<Diagnostic> diagnostics, SyntaxNode caseDeclaration, bool useOverride)
        {
            var storageCap = GetStorageCapable(type);
            var storageOverride = useOverride ? GetStorageOverride(type, caseDeclaration) : StorageKind.None;

            // storage annotations can override the default storage kind when it is trusted or untrusted, but not when impossible
            switch (storageOverride)
            {
                case StorageKind.Box:
                    if (!storageCap.HasFlag(StorageCapable.Boxable))
                    {
                        ReportNonBoxableCase(type, diagnostics, caseDeclaration);
                        return StorageKind.Isolate;
                    }
                    return StorageKind.Box;
                case StorageKind.Decompose:
                    if (!storageCap.HasFlag(StorageCapable.Decomposable) 
                        && GetDecomposableTrust(type) == TrustLevel.NotPossible)
                    {
                        ReportNonDecomposableCase(type, diagnostics, caseDeclaration);
                        return StorageKind.Isolate;
                    }
                    // otherwise, annotation == trust me bro
                    return StorageKind.Decompose;
                case StorageKind.Isolate:
                    if (type.IsReferenceType)
                    {
                        // don't isolate, use box instead
                        return StorageKind.Box;
                    }
                    return StorageKind.Isolate;
                case StorageKind.Overlap:
                    if (!storageCap.HasFlag(StorageCapable.Overlappable)
                        && GetOverlappableTrust(type) == TrustLevel.NotPossible)
                    {
                        ReportNonOverlappableCase(type, diagnostics, caseDeclaration);
                        return StorageKind.Isolate;
                    }
                    return StorageKind.Overlap;
                case StorageKind.None:
                default:
                    if (storageCap.HasFlag(StorageCapable.Overlappable))
                    {
                        return StorageKind.Overlap;
                    }
                    else if (storageCap.HasFlag(StorageCapable.Decomposable))
                    {
                        return StorageKind.Decompose;
                    }
                    else if (type.IsReferenceType)
                    {
                        return StorageKind.Box;
                    }
                    else
                    {
                        return StorageKind.Isolate;
                    }
            }
        }


        /// <summary>
        /// Gets the <see cref="TypeDescKind"/> for the type.
        /// </summary>
        private static TypeDescKind GetTypeDescKind(ITypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case CATypeKind.Enum:
                    return TypeDescKind.Primitive;
                case CATypeKind.Struct:
                    if (IsPrimitiveStruct(type))
                    {
                        return TypeDescKind.Primitive;
                    }
                    else if (type.IsRefLikeType)
                    {
                        return TypeDescKind.RefStruct;
                    }
                    else
                    {
                        return TypeDescKind.Struct;
                    }
                case CATypeKind.Interface:
                    return TypeDescKind.Interface;
                case CATypeKind.Class:
                case CATypeKind.Array:
                case CATypeKind.Dynamic:
                case CATypeKind.Delegate:
                    return TypeDescKind.Class;                
                case CATypeKind.TypeParameter:
                    var tp = (ITypeParameterSymbol)type;
                    if (tp.HasReferenceTypeConstraint)
                        return TypeDescKind.ClassTypeParameter;
                    else if (tp.HasValueTypeConstraint)
                        return TypeDescKind.StructTypeParameter;
                    else
                        return TypeDescKind.UnconstrainedTypeParameter;
                default:
                    return TypeDescKind.Unknown;
            }
        }

        private enum TrustLevel
        {
            Trusted,
            NotTrusted,
            NotPossible
        }

        /// <summary>
        /// True if the type is trusted to be overlapped with other overlappable types.
        /// </summary>
        private static bool IsTrustedOverlappableType(ITypeSymbol type)
        {
            return GetOverlappableTrust(type) == TrustLevel.Trusted;
        }

        /// <summary>
        /// Gets the overlap trust level for the given type.
        /// </summary>
        private static TrustLevel GetOverlappableTrust(ITypeSymbol type)
        {
            if (!type.IsValueType)
                return TrustLevel.NotPossible;

                if (type.TypeKind == CATypeKind.Enum
                    || IsPrimitiveStruct(type))
                {
                    return TrustLevel.Trusted;
                }

            if (type.IsNullable())
                return GetOverlappableTrust(type.GetNonNullableType());

            // must be declared in source to be certain that all fields are actually represented in the metadata
            if (type.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic).Any(f => !IsTrustedOverlappableType(f.Type)))
            {
                return TrustLevel.NotPossible;            
            }

            return TrustLevel.Trusted;
        }

        /// <summary>
        /// Returns true if the type is a struct that is trusted to be decomposable into its public fields and properties.
        /// </summary>
        private static bool IsTrustedDecomposableType(ITypeSymbol type)
        {
            return GetDecomposableTrust(type) == TrustLevel.Trusted;
        }

        /// <summary>
        /// Gets the decomposable trust level for the given type.
        /// </summary>
        private static TrustLevel GetDecomposableTrust(ITypeSymbol type)
        {
            // tuples are always trusted decomposable
            if (type.IsValueType 
                && type.IsTupleType)
                return TrustLevel.NotPossible;

            // primitives cannot be decomposed
            if (IsPrimitiveStruct(type))
                return TrustLevel.NotPossible;

            // trust only records with simple primary constructors and/or all public settable properties declared in source.
            if (type.IsValueType
                && type is INamedTypeSymbol nt)
            {
                var constructors = type.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor).ToList();
                var hasDefaultConstructor = constructors.Count == 0 || constructors.Any(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length == 0);

                if (type.IsRecord)
                {
                    // if record than trust that primary constructor parameters are associated with properties.
                    // otherwise requires that all public properties and fields are settable.
                    return HasOnlyPublicSettableMembers(type) 
                        ? TrustLevel.Trusted 
                        : TrustLevel.NotTrusted;
                }
                else
                {
                    if (!hasDefaultConstructor)
                        return TrustLevel.NotPossible;
                    return HasOnlyPublicSettableMembers(type) 
                        ? TrustLevel.Trusted 
                        : TrustLevel.NotTrusted;
                }
            }

            return TrustLevel.NotPossible;
        }

        /// <summary>
        /// Returns true if the type has only public fields and properties that have a set method (or init method).
        /// </summary>
        private static bool HasOnlyPublicSettableMembers(ITypeSymbol type)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is IFieldSymbol field)
                {
                    // static fields are not part of the decomposition, so ignore them
                    // auto-properties have implicitly declared fields, so ignore them
                    if (field.IsStatic
                        || field.IsImplicitlyDeclared)
                        continue;
                    if (field.DeclaredAccessibility != Accessibility.Public)
                        return false;
                }
                else if (member is IPropertySymbol prop)    
                {
                    // static properties are not part of the decomposition, so ignore them
                    if (prop.IsStatic)
                        continue;
                    // properties must be public and settable
                    if (prop.DeclaredAccessibility != Accessibility.Public || prop.SetMethod == null)
                        return false;
                }
            }

            return true;
        }

#if false
        private static bool IsSimpleSourceRecordStruct(ITypeSymbol type)
        {
            if (type.IsValueType
                && type.IsRecord 
                && type.IsDeclaredInSource())  // if its not in source, we don't know what shenanigans the author has done to the type.
            {
                // check that all property and field declarations are approved
                foreach (var syntaxRef in type.DeclaringSyntaxReferences)
                {
                    if (syntaxRef.GetSyntax() is RecordDeclarationSyntax recordDecl)
                    {
                        // all members must be public settable/initable auto properties or fields
                        foreach (var member in recordDecl.Members)
                        {
                            if (member is PropertyDeclarationSyntax propDecl)
                            {
                                // static properties are okay because they are not part of the decomposition
                                if (propDecl.Modifiers.Any( m => m.IsKind(SyntaxKind.StaticKeyword)))
                                    continue;
                                // only public declared properties are allowed to be part of the decomposition.
                                if (!propDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                                    return false;
                                // must have a set or init accessor
                                if (propDecl.AccessorList == null
                                    || !propDecl.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration)))
                                    return false;
                                // must be an auto-property
                                return propDecl.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null);
                            }
                            else if (member is FieldDeclarationSyntax fieldDecl)
                            {
                                // static fields are okay since they are not part of the decomposition.
                                if (fieldDecl.Modifiers.Any( m => m.IsKind(SyntaxKind.StaticKeyword)))
                                    continue;
                                // must be a public declared field to be part of the decomposition.
                                if (!fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                                    return false;
                                // if there are any non-public fields, then this is not a trusted decomposable record.
                                return false;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }                   
                    else
                    {
                        // not a record declaration syntax?
                        return false;
                    }
                }
            }

            return false;
        }
#endif        

        private static bool IsPrimitiveStruct(ITypeSymbol type)
        {
            if (!(type is INamedTypeSymbol nt))
                return false;

            switch (nt.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_DateTime:
                    return true;

                default:
                    return false;
            }
        }

        private static string GetModifiers(ISymbol symbol)
        {
            // try to get full set of modifiers as in source
            var location = symbol.Locations.FirstOrDefault(loc => loc.IsInSource);
            if (location != null && location.SourceTree != null)
            {
                var declNode = location.SourceTree.GetRoot().FindNode(location.SourceSpan);
                switch (declNode)
                {
                    case StructDeclarationSyntax decl:
                        return GetModifiers(decl.Modifiers);
                    case ClassDeclarationSyntax decl:
                        return GetModifiers(decl.Modifiers);
                    case RecordDeclarationSyntax decl:
                        return GetModifiers(decl.Modifiers);
                    case InterfaceDeclarationSyntax decl:
                        return GetModifiers(decl.Modifiers);
                    case MethodDeclarationSyntax decl:
                        return GetModifiers(decl.Modifiers);
                    case PropertyDeclarationSyntax decl:
                        return GetModifiers(decl.Modifiers);
                    case FieldDeclarationSyntax decl:
                        return GetModifiers(decl.Modifiers);
                }
            }

            return GetAccessibility(symbol.DeclaredAccessibility);

        }

        private static string GetModifiers(SyntaxTokenList modifiers)
        {
            // don't use actual source, so we are not dependent on trivia.
            return string.Join(" ", modifiers.Select(m => m.Text));
        }

        /// <summary>
        /// Returns the accessibilty that should be used for members that refer to this type in their signature.
        /// </summary>
        private static string GetMemberAccessibilityForType(ITypeSymbol symbol)
        {
            if (symbol.DeclaredAccessibility == Accessibility.Public)
            {
                if (symbol.ContainingType != null)
                {
                    return GetMemberAccessibilityForType(symbol.ContainingType);
                }
                else
                {
                    return "public";
                }
            }

            // if type is not public then the generated member must not be declared public or suffer the wrath of C#.
            // internal is okay, since otherwise the type is somehow accessible to the location the union type is being generated to.
            return "internal";
        }

        /// <summary>
        /// Gets the accessibility as C# text.
        /// </summary>
        private static string GetAccessibility(Accessibility acc)
        {
            switch (acc)
            {
                case Accessibility.Public:
                    return "public";
                case Accessibility.Protected:
                    return "protected";
                case Accessibility.Private:
                    return "private";
                case Accessibility.Internal:
                    return "internal";
                case Accessibility.ProtectedAndInternal:
                    return "private protected";
                case Accessibility.ProtectedOrInternal:
                    return "internal protected";
                default:
                    return "";
            }
        }

        private static string GetTypeFullName(ITypeSymbol type)
        {
            var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // allows use ? for annoted type names
            if (type.NullableAnnotation == NullableAnnotation.Annotated
                && !name.EndsWith("?"))
            {
                name += "?";
            }

            return name;
        }

        private static string GetTypeShortName(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol nt)
            {
                var typeParameterList = GetTypeParameterList(nt);
                return string.IsNullOrEmpty(typeParameterList) ? nt.Name : nt.Name + typeParameterList;
            }
            else if (type is IArrayTypeSymbol at)
            {
                var elementType = GetTypeShortName(at.ElementType);
                if (at.Rank == 1)
                {
                    return elementType + "[]";
                }
                else if (at.Rank == 2)
                {
                    return elementType + "[,]";
                }
                else
                {
                    return elementType + "[" + new string(',', at.Rank - 1) + "]";
                }
            }
            else
            {
                return type.Name;
            }
        }

        private static string GetTypeParameterList(INamedTypeSymbol type)
        {
            if (type.TypeParameters.Length > 0)
            {
                return $"<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>";
            }
            else
            {
                return "";
            }
        }

        private static string GetNamespaceName(INamespaceSymbol ns)
        {
            if (ns.ContainingNamespace != null
                && !ns.ContainingNamespace.IsGlobalNamespace)
            {
                return GetNamespaceName(ns.ContainingNamespace) + "." + ns.Name;
            }

            return ns.Name;
        }

        private static bool ContainsInTrivia(ISymbol symbol, string text)
        {
            return GetDeclarationNodes(symbol).Any(d => ContainsInTrivia(d, text));
        }

        private static bool ContainsInTrivia(SyntaxNode node, string text)
        {
            var commentTrivia = node.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)).ToArray();
            return commentTrivia.Any(t => t.ToString().Contains(text));
        }

        private static bool TryGetCommentProperty(SyntaxNode node, string propertyName, out string? value)
        {
            value = null;
            var commentTrivia = node.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)).ToArray();
            foreach (var trivia in commentTrivia)
            {
                var text = trivia.ToString();
                var prefix = "@" + propertyName;
                var startIndex = text.IndexOf(prefix);
                if (startIndex >= 0)
                {
                    var endOfPrefix = startIndex + prefix.Length;

                    if (endOfPrefix < text.Length && text[endOfPrefix] == '=')
                    {
                        startIndex = endOfPrefix + 1;
                        var endIndex = text.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }, startIndex);
                        if (endIndex < 0)
                            endIndex = text.Length;
                        value = text.Substring(startIndex, endIndex - startIndex);
                        return true;
                    }
                    else if (endOfPrefix == text.Length 
                        || text.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }, endOfPrefix) >= endOfPrefix)
                    {
                        value = "true";
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryGetCommentProperty<T>(SyntaxNode node, string propertyName, out T? value)
        {
            value = default;
            if (TryGetCommentProperty(node, propertyName, out var strValue))
            {
                try
                {
                    value = (T)Convert.ChangeType(strValue, typeof(T));
                    return true;
                }
                catch
                {
                    // ignore conversion errors and just return false
                }
            }
            return false;
        }

        private static bool TryGetCommentProperty(ISymbol symbol, string propertyName, out string? value)
        {
            value = null;
            foreach (var node in GetDeclarationNodes(symbol))
            {
                if (TryGetCommentProperty(node, propertyName, out value))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetCommentProperty<T>(ISymbol symbol, string propertyName, out T? value)
        {
            value = default;
            foreach (var node in GetDeclarationNodes(symbol))
            {
                if (TryGetCommentProperty(node, propertyName, out value))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<SyntaxNode> GetDeclarationNodes(ISymbol symbol)
        {
            foreach (var location in symbol.Locations.Where(loc => loc.IsInSource))
            {
                if (location.SourceTree is SyntaxTree sourceTree
                    && sourceTree.GetRoot() is SyntaxNode root)
                {
                    var declaration = root.FindNode(location.SourceSpan);
                    if (declaration != null)
                        yield return declaration;
                }
            }
        }

        private static void ReportUnsupportedCaseTypes(ITypeSymbol type, List<Diagnostic> diagnostics, SyntaxNode? caseDeclaration)
        {
            var typeKind = GetTypeDescKind(type);
            if (typeKind == TypeDescKind.Unknown
                || typeKind == TypeDescKind.RefStruct)
            {
                var location = caseDeclaration?.GetLocation() ?? type.Locations.FirstOrDefault();
                diagnostics.Add(Diagnostic.Create(UnsupportedCaseTypeDiagnostic, location, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }
            else
            {
                // check if the type contains any unsupported member types, and report diagnostics for those as well.
                foreach (var field in type.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic))
                {
                    ReportUnsupportedCaseTypes(field.Type, diagnostics, caseDeclaration);
                }               
            }
        }

        private static void ReportNonOverlappableCase(ITypeSymbol type, List<Diagnostic> diagnostics, SyntaxNode? caseDeclaration)
        {
            var location = caseDeclaration?.GetLocation() ?? type.Locations.FirstOrDefault();
            diagnostics.Add(Diagnostic.Create(NonOverlappableCaseDiagnostic, location, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        private static void ReportNonDecomposableCase(ITypeSymbol type, List<Diagnostic> diagnostics, SyntaxNode? caseDeclaration)
        {
            var location = caseDeclaration?.GetLocation() ?? type.Locations.FirstOrDefault();
            diagnostics.Add(Diagnostic.Create(NonDecomposableCaseDiagnostic, location, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        private static void ReportNonBoxableCase(ITypeSymbol type, List<Diagnostic> diagnostics, SyntaxNode? caseDeclaration)
        {
            var location = caseDeclaration?.GetLocation() ?? type.Locations.FirstOrDefault();
            diagnostics.Add(Diagnostic.Create(NonBoxableCaseDiagnostic, location, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        private static DiagnosticDescriptor UnsupportedCaseTypeDiagnostic = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            "UT0001",
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Unsupported case type",
            "The union contains the unsupported type '{0}'. Only primitive types, structs, records, tuples, classes, interfaces and arrays are supported in unions.",
            "UnionTypes",
            DiagnosticSeverity.Error,
            true
            );

        private static DiagnosticDescriptor NonOverlappableCaseDiagnostic = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            "UT0002",
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Non-overlappable case type",
            "The case type '{0}' cannot be overlapped with other cases in the union, it will be isolated or decomposed if possible",
            "UnionTypes",
            DiagnosticSeverity.Warning,
            true
            );

        private static DiagnosticDescriptor NonDecomposableCaseDiagnostic = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            "UT0003",
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Non-decomposable case type",
            "The case type '{0}' cannot be decomposed into its member values, it will be isolated into its own field",
            "UnionTypes",
            DiagnosticSeverity.Warning,
            true
            );

       private static DiagnosticDescriptor NonBoxableCaseDiagnostic = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            "UT0004",
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Non-boxable case type",
            "The case type '{0}' cannot be boxed into an object field, it will be isolated into a field of its own type",
            "UnionTypes",
            DiagnosticSeverity.Warning,
            true
            );

        [Flags]
        public enum StorageCapable
        {
            // note: all types are isolate capable.

            /// <summary>
            /// The type has no special storage capabilities (except isolate)
            /// </summary>
            None = 0,

            /// <summary>
            /// The type is boxable (can be stored in a shared object field)
            /// </summary>
            Boxable = 1 << 0,

            /// <summary>
            /// The type is decomposable into its constituent members and stored separately.
            /// </summary>
            Decomposable = 1 << 1,

            /// <summary>
            /// The type is overlappable with other overlappable types
            /// </summary>
            Overlappable = 1 << 2,
        }
    }
}