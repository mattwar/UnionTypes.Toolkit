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

        private static bool IsCustomUnion(INamedTypeSymbol symbol, out AttributeData? attribute)
        {
            // verify that the symbol truly has the C# [Union] attribute applied,
            // and its not just some other attribute with the same name
            if (symbol.TryGetAttribute("System.Runtime.CompilerServices.UnionAttribute", out attribute))
                return true;
            attribute = null;
            return false;
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

            var options = GetLayoutOptionsFromComments(unionType);

            // get all cases declared for union type
            //GetTypeCasesFromNestedTypes(unionType, cases, diagnostics);
            GetTypeCasesFromPrivateCaseMethod(unionType, cases, diagnostics);

            if (cases.Count > 0)
            {
                var name = unionType.Name; // name w/o type parameters or namespace
                var typeName = GetTypeShortName(unionType); // name w/o namespace

                var union = new UnionInfo(
                    name,
                    options,
                    cases
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
        /// Gets <see cref="LayoutOptions"/> from comments on the declared union type.
        /// </summary>
        private LayoutOptions GetLayoutOptionsFromComments(ISymbol symbol)
        {
            var options = LayoutOptions.Default;

            if (TryGetCommentProperty(symbol, "UnionLayout", out var value))
            {
                if (Enum.TryParse(value, ignoreCase: true, out LayoutOptions parsed))
                {
                    options = parsed;
                }
            }

            return options;
        }

        private void GetTypeCasesFromNestedTypes(
            INamedTypeSymbol unionType, 
            List<CaseDesc> cases,
            List<Diagnostic> diagnostics)
        {
            var nestedTypes = unionType
                .GetTypeMembers()
                .OfType<INamedTypeSymbol>()
                .Where(nt => nt.DeclaredAccessibility == Accessibility.Public
                        || nt.DeclaredAccessibility == Accessibility.Internal)
                .ToList();

            foreach (var nestedType in nestedTypes)
            {
                if (nestedType.TryGetAttribute(CaseAttributeName, out var attr))
                {
                    var caseDesc = GetCaseDesc(nestedType);
                    cases.Add(caseDesc);
                }
            }
        }

        private void GetTypeCasesFromPrivateCaseMethod(
            INamedTypeSymbol unionType, 
            List<CaseDesc> cases,
            List<Diagnostic> diagnostics)
        {
            // find all "Case" methods that are private static void and have at least one parameter
            // use these to determine the set of case from the parameter types.
            var casesMethods = unionType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => (m.Name == "Cases" || m.Name == "Case")
                         && m.DeclaredAccessibility == Accessibility.Private
                         && m.TypeParameters.Length == 0
                         && m.ReturnType != null && m.ReturnType.SpecialType == SpecialType.System_Void
                         && m.Parameters.Length > 0)
                .ToList();

            foreach (var cm in casesMethods)
            {
                foreach (var pm in cm.Parameters)
                {
                    var caseDesc = GetCaseDesc(pm.Type);
                    cases.Add(caseDesc);
                }
            }
        }

        /// <summary>
        /// Gets a deconstructable member of a case type from a parameter symbol,
        /// typically from the constructor of the type (record)
        /// </summary>
        private MemberDesc GetCaseMember(string name, ITypeSymbol type, bool isParameter)
        {
            var kind = GetTypeDescKind(type);
            var typeName = GetTypeFullName(type);
            var nestedMembers = GetDecomposibleMembers(type);
            var typeDesc = nestedMembers.Count > 0 ? new TypeDesc(typeName, nestedMembers) : new TypeDesc(typeName, kind);
            var memberDesc = new MemberDesc(name, typeDesc, isParameter);
            return memberDesc;
        }

        private CaseDesc GetCaseDesc(ITypeSymbol type)
        {
            var kind = GetTypeDescKind(type);
            var typeName = GetTypeFullName(type);
            var members = GetDecomposibleMembers(type);
            var typeDesc = members.Count > 0 ? new TypeDesc(typeName, members) : new TypeDesc(typeName, kind);
            return new CaseDesc(typeDesc, generateType: false);
        }

        private IReadOnlyList<MemberDesc> GetDecomposibleMembers(ITypeSymbol caseSymbol)
        {
            if (caseSymbol.IsValueType
                && caseSymbol is INamedTypeSymbol nt)
            {
                if (caseSymbol.IsRecord)
                {
                    // break down record into members based on primary constructor..
                    // For records, the names of the parameters are the same as the names of the properties.
                    var primaryConstructor = nt.Constructors.FirstOrDefault(c => c.Parameters.Length > 0);
                    if (primaryConstructor != null && primaryConstructor.Parameters.Length > 0)
                    {
                        return primaryConstructor.Parameters.Select(p => GetCaseMember(p.Name, p.Type, isParameter: true)).ToArray();
                    }
                    else
                    {
                        // if no primary constructor, use public settable properties as members
                        nt.GetMembers()
                            .OfType<IPropertySymbol>()
                            .Where(p => p.DeclaredAccessibility == Accessibility.Public
                                && !p.IsStatic
                                && p.GetMethod != null
                                && p.SetMethod != null)
                            .Select(p => GetCaseMember(p.Name, p.Type, isParameter: false))
                            .ToArray();                       
                    }
                }
                else if (caseSymbol.IsTupleType)
                {
                    var constructor = nt.Constructors.FirstOrDefault(c => c.Parameters.Length > 0);
                    if (constructor != null)
                    {
                        return constructor.Parameters.Select(p => GetCaseMember(GetTuplePropertyName(p), p.Type, isParameter: true)).ToArray();
                    }
                }
            }

            return Array.Empty<MemberDesc>();
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
                    else if (type.IsTupleType)
                    {
                        // trust value tuples to not have hidden metadata
                        if (IsOverlappableStruct(type))
                        {
                            return TypeDescKind.OverlappableStruct;
                        }
                        else
                        {
                            return TypeDescKind.DecomposableStruct;
                        }
                    }
                    else if (type.IsRecord)
                    {
                        var isDeconstructable = IsDecomposableStruct(type);
                        var isOverlappable = IsOverlappableStruct(type);
                        
                        // only trust records to be overlappable if they are declared in source
                        // such that we are identify all fields
                        // if (type.IsDeclaredInSource())
                        // {
                            if (isOverlappable)
                            {
                                return TypeDescKind.OverlappableStruct;
                            }
                            else if (isDeconstructable)
                            {
                                return TypeDescKind.DecomposableStruct;
                            }
                            else
                            {
                                return TypeDescKind.Struct;
                            }
                            // }
                            // else
                            // {
                            //     if (isDeconstructable)
                            //     {
                            //         return TypeKind.DeconstructableStruct;
                            //     }
                            //     else
                            //     {
                            //         return TypeKind.Struct;
                            //     }
                            // }
                    }
                    else if (type.IsRefLikeType)
                    {
                        // We don't actually handle this kind of type, but we will treat it as a struct for now, since it is a value type.
                        return TypeDescKind.Struct;
                    }
                    else if (IsOverlappableStruct(type))
                    {
                        // if (type.IsDeclaredInSource())
                        // {
                        //     return TypeKind.OverlappableLocalStruct;
                        // }
                        // else
                        // {
                        //     return TypeKind.OverlappableForeignStruct;
                        // }
                        return TypeDescKind.OverlappableStruct;
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
                        return TypeDescKind.Class;
                    else if (tp.HasValueTypeConstraint)
                        return TypeDescKind.Struct;
                    else
                        return TypeDescKind.TypeParameter;
                default:
                    return TypeDescKind.Struct;
            }
        }

        private static bool IsOverlappableType(ITypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case CATypeKind.Enum:
                    return true;
                case CATypeKind.Struct:
                    return IsPrimitiveStruct(type)
                        || IsOverlappableStruct(type);
                default:
                    return false;
            }
        }

        private static bool IsOverlappableStruct(ITypeSymbol type)
        {
            return type.IsValueType
                && type.GetMembers().OfType<IFieldSymbol>().All(f => IsOverlappableType(f.Type));
        }

        private static bool IsDecomposableStruct(ITypeSymbol type)
        {
            if (type.IsValueType && (type.IsRecord || type.IsTupleType))
            {
                // all records and tuples are decomposible.               
                return true;
            }

            return false;
        }

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

        private static DiagnosticDescriptor DuplicateCaseNameDiagnostic = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            "UT0001",
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Duplicate case name",
            "Case name '{0}' is already used in the union type",
            "UnionTypes",
            DiagnosticSeverity.Error,
            true
            );


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
    }
}