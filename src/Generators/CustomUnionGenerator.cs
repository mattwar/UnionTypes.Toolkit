// <#+
#if !T4
namespace UnionTypes.Toolkit.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#endif

#nullable enable

    public class CustomUnionGenerator : Generator
    {
        private static readonly string TagFieldName = "_kind";
        private static readonly string DataFieldPrefix = "_value";
        private static readonly string OverlappedFieldName = "_overlapped";
        private static readonly string OverlappedTypeName = "Overlapped";
        private static readonly string OverlappedCaseFieldName = "Case";
        private static readonly string AccessorNamePrefix = "GetCase";
        private static readonly string LocalVariablePrefix = "tmp";

        public CustomUnionGenerator()
        {
        }

        public string Generate(UnionInfo union)
        {
            WriteLine("using System;");
            WriteLine("using System.Collections.Generic;");
            WriteLine("using System.Diagnostics.CodeAnalysis;");
            WriteLine("using System.Runtime.CompilerServices;");
            WriteLine("using System.Runtime.InteropServices;");
            WriteLine("#nullable enable");
            WriteLine("#pragma warning disable CS8618");
            WriteLine();
            WriteUnionType(union);
            return GeneratedText;
        }

        /// <summary>
        /// Writes the declaration of the union type
        /// </summary>
        /// <param name="union"></param>
        private void WriteUnionType(UnionInfo union)
        {
            var layout = ComputeLayout(union);

            if (!string.IsNullOrEmpty(union.Namespace))
            {
                WriteLine($"namespace {union.Namespace}");
                WriteBraceNested(() =>
                {
                    Write();
                });
            }
            else
            {
                Write();
            }

            void Write()
            {
                WriteLine("[System.Runtime.CompilerServices.Union]");
                WriteLine($"public partial struct {union.DeclarationName} : System.Runtime.CompilerServices.IUnion");
                WriteBraceNested(() =>
                {
                    WriteLineSeparatedBlocks(() =>
                    {
                        WriteBlock(() => WriteStorageFields(layout));
                        WriteBlock(() => WriteOverlappedType(layout));
                        WriteBlock(() => WriteCaseTypes(layout));
                        WriteBlock(() => WriteConstructors(layout));
                        WriteBlock(() => WriteCaseAccessors(layout));
                        WriteBlock(() => WriteValueProperty(layout));
                        WriteBlock(() => WriteHasValue(layout));
                        WriteBlock(() => WriteTryGetValueMethods(layout));
                    });
                });
            }
        }

        /// <summary>
        /// Writes all field declares for case value storage: including the tag, overlapped and individual case fields.
        /// </summary>
        private void WriteStorageFields(UnionLayout layout)
        {
            if (layout.TagField != null)
                WriteLine($"private readonly {layout.TagField.Type.TypeName} {layout.TagField.Name};");

            if (layout.OverlappedField != null)
                WriteLine($"private readonly {layout.OverlappedField.Type.TypeName} {layout.OverlappedField.Name};");

            foreach (var field in layout.DataFields)
            {
                WriteLine($"private readonly {field.Type.TypeName} {field.Name};");
            }
        }

        /// <summary>
        /// Writes the declaration of the type that holds the overlapped elements of each case, if at least two cases have overlappable elements.
        /// </summary>
        private void WriteOverlappedType(UnionLayout layout)
        {
            if (layout.OverlappedField != null)
            {
                WriteLine($"[StructLayout(LayoutKind.Explicit)]");
                WriteLine($"private struct {OverlappedTypeName}");
                WriteBraceNested(() =>
                {
                    foreach (var caseLayout in layout.CaseLayouts)
                    {
                        if (caseLayout.OverlappedCaseField != null)
                        {
                            WriteLine($"[FieldOffset(0)] public {caseLayout.OverlappedCaseField.Type.TypeName} {caseLayout.OverlappedCaseField.Name};");
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Writes a declaration for any case types that require generation as a record struct.
        /// </summary>
        private void WriteCaseTypes(UnionLayout layout)
        {
            foreach (var caseInfo in layout.Union.Cases)
            {
                if (caseInfo.GenerateType)
                {
                    var members = string.Join(", ", caseInfo.Type.Members.Select(m => $"{m.Type.TypeName} {m.Name}"));
                    WriteLine($"public record struct {caseInfo.Type.TypeName}({members});");
                }
            }
        }

        /// <summary>
        /// Writes public constructors for all the case types.
        /// </summary>
        private void WriteConstructors(UnionLayout layout)
        {
            WriteLineSeparatedBlocks(() =>
            {
                foreach (var caseLayout in layout.CaseLayouts)
                {
                    WriteBlock(() => WriteConstructor(caseLayout));
                }
            });

            // The constructor assigns the value to its corresponding field, 
            // or decomposes it into its members and assigns those members to their corresponding fields.
            void WriteConstructor(CaseLayout caseLayout)
            {
                WriteLine($"public {layout.Union.SimpleName}({caseLayout.Type.TypeName} value)");
                WriteBraceNested(() =>
                {
                    if (caseLayout.Type.IsReference
                        || caseLayout.Type.MightBeNullable)
                    {
                        // null values become equivalent of default for the struct
                        WriteLine("if (value is null) return;");
                    }

                    if (layout.TagField != null)
                    {
                        WriteLine($"{layout.TagField.Name} = {caseLayout.TagValue};");
                    }

                    if (caseLayout.IsDecomposed)
                    {
                        Decompose(caseLayout);
                    }
                    else if (caseLayout.Field != null)
                    {
                        WriteFieldReference(caseLayout.Field);
                        WriteLine($" = value;");
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected layout writing constructor");
                    }
                });

                void Decompose(ElementLayout elementLayout)
                {
                    var locals = new List<LocalMember>();

                    DecomposeElement(caseLayout, "value");

                    if (locals.Count > 0)
                    {
                        for (int i = 0; i < locals.Count; i++)
                        {
                            var local = locals[i];
                            if (!local.Member.IsOverlapped)
                                DecomposeElement(local.Member, local.LocalName);
                        }

                        var overlappedLocals = locals.Where(loc => loc.Member.IsOverlapped).ToList();
                        if (overlappedLocals.Count > 0 && caseLayout.OverlappedCaseField != null)
                        {
                            var tuple = $"({string.Join(", ", overlappedLocals.Select(loc => loc.LocalName))})";
                            WriteFieldReference(caseLayout.OverlappedCaseField);
                            WriteLine($" = {tuple};");
                        }
                    }

                    void DecomposeElement(ElementLayout elementLayout, string source)
                    {
                        var parameterMembers = elementLayout.Members.Where(m => m.Member.IsParameter).ToList();
                        var propertyMembers = elementLayout.Members.Where(m => !m.Member.IsParameter).ToList();

                        // use deconstructor to access parameter members
                        if (parameterMembers.Count > 1)
                        {
                            // use deconstruction syntax is two or more members
                            Write("(");

                            for (int i = 0; i < parameterMembers.Count; i++)
                            {
                                var member = parameterMembers[i];
                                if (i > 0)
                                    Write(", ");

                                if (member.Field != null)
                                {
                                    WriteFieldReference(member.Field);
                                }
                                else if (member.IsDecomposed || member.IsOverlapped)
                                {
                                    // assign to local and handle later
                                    var localName = $"{LocalVariablePrefix}{locals.Count}";
                                    var local = new LocalMember(localName, member);
                                    locals.Add(local);
                                    Write($"var {localName}");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected layout writing constructor");
                                }
                            }

                            WriteLine($") = {source};");
                        }
                        else if (parameterMembers.Count == 1)
                        {
                            // if only one parameter, cannot use deconstruction syntax, so call method directly
                            var member = parameterMembers[0];
                            WriteLine($"{source}.Deconstruct(out var v);");

                            if (member.Field != null)
                            {
                                WriteFieldReference(member.Field);
                            }
                            else
                            {
                                var localName = $"{LocalVariablePrefix}{locals.Count}";
                                var local = new LocalMember(localName, member);
                                locals.Add(local);
                                Write(localName);
                            }

                            WriteLine(" = v;");
                        }

                        // access property members individually
                        if (propertyMembers.Count > 1)
                        {
                            for (int i = 0; i < propertyMembers.Count; i++)
                            {
                                var member = propertyMembers[i];
                                if (member.Field != null)
                                {
                                    WriteFieldReference(member.Field);
                                    Write(" = ");
                                    Write($"{source}.{member.Member.Name}");
                                    WriteLine(";");
                                }
                                else if (member.IsDecomposed || member.IsOverlapped)
                                {
                                    // assign to variable and handle later
                                    var localName = $"{LocalVariablePrefix}{locals.Count}";
                                    var local = new LocalMember(localName, member);
                                    locals.Add(local);
                                    Write($"var {localName} = {source}.{member.Member.Name};");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected layout writing constructor");
                                }
                            }
                        }
                    }                   
                }
            }
        }

        /// <summary>
        /// A local variable used to store a element value during construction/deconstruction of a case value.
        /// </summary>
        private class LocalMember
        {
            public string LocalName { get; }
            public ElementLayout Member { get; }
            public LocalMember(string localName, ElementLayout member)
            {
                LocalName = localName;
                Member = member;
            }
        }

        /// <summary>
        /// Writes accessor methods that reconstruct/access the case values.
        /// These accessors do not check the tag value.
        /// </summary>
        private void WriteCaseAccessors(UnionLayout layout)
        {
            if (layout.TagField == null)
                return;

            for (int i = 0; i < layout.CaseLayouts.Count; i++)
            {
                var caseLayout = layout.CaseLayouts[i];
                var locals = new List<LocalMember>();
                GatherLocals(caseLayout, locals);
                var map = locals.ToDictionary(loc => loc.Member);

                if (locals.Count > 0)
                {
                    WriteLine($"private {caseLayout.Type.TypeName} {AccessorNamePrefix}{i + 1}()");
                    WriteBraceNested(() =>
                    {
                        // multi-element overlapped data into locals
                        if (caseLayout.OverlappedCaseField != null)
                        {
                            var deconstruct = locals.Count > 1 
                                ? $"({string.Join(", ", locals.Select(loc => $"var {loc.LocalName}"))})"
                                : $"var {locals[0].LocalName}";
                            Write($"{deconstruct} = ");
                            WriteFieldReference(caseLayout.OverlappedCaseField);
                            WriteLine(";");
                        }

                        Write("return ");
                        AccessElement(caseLayout, map);
                        WriteLine(";");
                    });
                }
                else
                {
                    Write($"private {caseLayout.Type.TypeName} {AccessorNamePrefix}{i + 1}() => ");
                    AccessElement(caseLayout, map);
                    WriteLine(";");
                }
            }

            // determines which members need to be stored in local variables for reconstruction
            void GatherLocals(ElementLayout elementLayout, List<LocalMember> locals)
            {
                if (elementLayout.IsDecomposed)
                {
                    foreach (var member in elementLayout.Members)
                    {
                        GatherLocals(member, locals);
                    }
                }
                else if (elementLayout.IsOverlapped 
                    && elementLayout is MemberLayout memberLayout)
                {
                    var localName = $"{LocalVariablePrefix}{locals.Count}";
                    locals.Add(new LocalMember(localName, memberLayout));
                }
            }

            // Accesses an element from its storage field(s) or local
            void AccessElement(ElementLayout elementLayout, Dictionary<ElementLayout, LocalMember> localsMap)
            {
                if (localsMap.TryGetValue(elementLayout, out var local))
                {
                    Write(local.LocalName);
                }
                else if (elementLayout.IsDecomposed)
                {
                    Recompose(elementLayout, localsMap);
                }
                else if (elementLayout.Field != null)
                {
                    if (elementLayout.Field.Type != elementLayout.Type)
                        Write($"({elementLayout.Type.TypeName})");

                    WriteFieldReference(elementLayout.Field);
                }
                else
                {
                    throw new InvalidOperationException("Unexpected layout in accessor");
                }
            }

            // Reconstructs a decomposed element or case from its stored members
            void Recompose(ElementLayout elementLayout, Dictionary<ElementLayout, LocalMember> localsMap)
            {
                var parameterMembers = elementLayout.Members.Where(m => m.Member.IsParameter).ToList();
                var propertyMembers = elementLayout.Members.Where(m => !m.Member.IsParameter).ToList();

                if (parameterMembers.Count > 0)
                {
                    Write("new (");
                    
                    for (int i = 0; i < parameterMembers.Count; i++)
                    {
                        if (i > 0)
                            Write(", ");

                        var memberLayout = parameterMembers[i];
                        AccessElement(memberLayout, localsMap);
                    }

                    Write(")");                       

                    if (propertyMembers.Count > 0)
                    {
                        Write(" { ");
                        for (int i = 0; i < propertyMembers.Count; i++)
                        {
                            if (i > 0)
                                Write(", ");

                            var memberLayout = propertyMembers[i];
                            Write($"{memberLayout.Member.Name} = ");
                            AccessElement(memberLayout, localsMap);
                        }

                        Write(" }");
                    }
                }               
            }
        }

        /// <summary>
        /// Writes a full path access expression to a field.
        /// </summary>
        private void WriteFieldReference(DataField field)
        {
            if (field.Path != null)
            {
                WriteFieldReference(field.Path);
                Write(".");
            }
            Write(field.Name);
        }

        /// <summary>
        /// Gets the expression that calls the case accessor for the specified case.
        /// </summary>
        private string GetCaseAccessExpression(UnionLayout layout, CaseLayout caseLayout)
        {
            if (layout.TagField == null
                && layout.DataFields.Count == 1)
            {
                // it is stored in its own field, just return the field value.
                return $"{layout.DataFields[0].Name}";
            }
            else
            {
                // it is accessed via its 'access' member
                var i = IndexOf(layout.CaseLayouts, caseLayout);
                return $"this.{AccessorNamePrefix}{i+1}()";
            }
        }


        /// <summary>
        /// Gets the index of an item in a list, using default equality comparer.
        /// </summary>
        private static int IndexOf<T>(IReadOnlyList<T> items, T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < items.Count; i++)
            {
                if (comparer.Equals(items[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Generates the union's Value property
        /// </summary>
        private void WriteValueProperty(UnionLayout layout)
        {
            WriteLine($"public object? Value =>");
            WriteNested(() =>
            {
                if (layout.TagField != null)
                {
                    // the type has a tag field, so use a switch expression to access/reconstruct the appropriate case value.`
                    WriteLine($"{layout.TagField.Name} switch");
                    WriteLine("{");
                    WriteNested(() =>
                    {
                        foreach (var caseLayout in layout.CaseLayouts)
                        {
                            var caseValue = GetCaseAccessExpression(layout, caseLayout);
                            WriteLine($"{caseLayout.TagValue} => {caseValue},");
                        }
                        WriteLine("_ => null");
                    });
                    WriteLine("};");
                }
                else if (layout.DataFields.Count == 1)
                {
                    // the value is only ever stored in exactly one field (boxed layout) so just return that field value.
                    WriteFieldReference(layout.DataFields[0]);
                    WriteLine(";");
                }
                else
                {
                    // some other encoding not implemented yet?
                    throw new NotImplementedException();
                }
            });
        }

        /// <summary>
        /// Generates a HasValue property for tagged layout models.
        /// </summary>
        private void WriteHasValue(UnionLayout layout)
        {
            if (layout.TagField != null)
            {
                Write($"public bool HasValue => ");
                WriteFieldReference(layout.TagField);
                WriteLine(" != 0;");
            }
        }

        /// <summary>
        /// Generates TryGetValue methods for tagged layout models.
        /// </summary>
        private void WriteTryGetValueMethods(UnionLayout layout)
        {
            // only generate these when there is non-boxing
            if (layout.TagField == null)
                return;

            WriteLineSeparatedBlocks(() =>
            {
                foreach (var caseLayout in layout.CaseLayouts)
                {
                    WriteBlock(() => Write(caseLayout));
                }
            });

            void Write(CaseLayout caseLayout)
            {
                WriteLine($"public bool TryGetValue([NotNullWhen(true)] out {caseLayout.Type.TypeName} value)");
                WriteBraceNested(() =>
                {
                    WriteLine($"if ({layout.TagField.Name} == {caseLayout.TagValue})");
                    WriteBraceNested(() =>
                    {
                        var caseValue = GetCaseAccessExpression(layout, caseLayout);
                        WriteLine($"value = {caseValue};");
                        WriteLine("return true;");
                    });
                    WriteLine("else");
                    WriteBraceNested(() =>
                    {
                        WriteLine("value = default!;");
                        WriteLine("return false;");
                    });
                });
            }
        }

        #region layout

        /// <summary>
        /// Computes the layout information for the unios based on options and cases
        /// </summary>
        private UnionLayout ComputeLayout(UnionInfo union)
        {
            if ((union.Options & LayoutOptions.AllowBoxing) != 0)
                return ComputeBoxedLayout(union);

            return ComputeTaggedLayout(
                union,
                allowReferenceSharing: (union.Options & LayoutOptions.AllowReferenceFieldSharing) != 0,
                allowFieldSharing:     (union.Options & LayoutOptions.AllowFieldSharing) != 0,
                allowDecomposition:    (union.Options & LayoutOptions.AllowDecomposition) != 0,
                allowOverlap:          (union.Options & LayoutOptions.AllowOverlappingFields) != 0
                );
        }


        /// <summary>
        /// Computes the layout that boxes all values into a single object field.
        /// </summary>
        private UnionLayout ComputeBoxedLayout(UnionInfo union)
        {
            var valueField = new DataField(DataFieldPrefix, TypeDesc.Object.Nullable);
            var dataFields = new List<DataField>() { valueField };

            var caseLayouts = new List<CaseLayout>();

            foreach (var caseDesc in union.Cases)
            {
                var layout = new CaseLayout(caseDesc, "", valueField);
                caseLayouts.Add(layout);
            }

            return new UnionLayout(
                union, 
                caseLayouts, 
                dataFields
                );
        }

        /// <summary>
        /// Computes the layout that has a tag and separate/overlapped storage for cases and/or their decomposed elements
        /// </summary>
        /// <param name="union">The union information specified by the user.</param>
        /// <param name="allowReferenceSharing">Indicates whether reference fields can be shared among cases.</param>
        /// <param name="allowFieldSharing">Indicates whether value fields of the same type can be shared among cases.</param>
        /// <param name="allowDecomposition">Indicates whether cases can be decomposed into their constituent elements.</param>
        /// <param name="allowOverlap">Indicates whether overlapping of overlappable fields is allowed.</param>
        /// <returns>The computed layout for the union.</returns>
        private UnionLayout ComputeTaggedLayout(
            UnionInfo union,
            bool allowReferenceSharing,
            bool allowFieldSharing,
            bool allowDecomposition,
            bool allowOverlap)
        {
            var tagField = new DataField(TagFieldName, TypeDesc.Int32);
            var dataFields = new List<DataField>();

            DataField? overlappedField = null;
            if (allowOverlap
                && union.Cases.Count(c => HasOverlappableMembers(c.Type)) >= 2)
            {
                overlappedField = new DataField(OverlappedFieldName, new TypeDesc(OverlappedTypeName, TypeDescKind.Struct));
            }

            var caseLayouts = new List<CaseLayout>();

            var referenceCount = union.Cases.Count(c => c.Type.IsReference);
            var usedFields = new HashSet<DataField>();

            for (int i = 0; i < union.Cases.Count; i++)
            {
                var caseDesc = union.Cases[i];
                var caseType = caseDesc.Type;
                usedFields.Clear();

                if (caseType.IsDecomposible && allowDecomposition)
                {
                    DataField? overlappedCaseField = null;
                    var overlappableMembers = GetOverlappableMembers(caseType.Members);
                    if (overlappedField != null
                        && overlappableMembers.Count > 0)
                    {
                        var fieldType = GetOverlappedCaseType(overlappableMembers);
                        overlappedCaseField = new DataField($"{OverlappedCaseFieldName}{i + 1}", fieldType, overlappedField);
                    }
                    var memberLayouts = CreateMemberLayouts(caseType.Members, overlappedCaseField, overlappableMembers.Count);
                    var layout = new CaseLayout(caseType, $"{i + 1}", null, overlappedCaseField, true, memberLayouts);
                    caseLayouts.Add(layout);
                }
                else if (caseType.IsOverlappable && overlappedField != null)
                {
                    var overlappedCaseField = new DataField($"{OverlappedCaseFieldName}{i + 1}", caseType, overlappedField);
                    var layout = new CaseLayout(caseType, $"{i + 1}", overlappedCaseField, overlappedCaseField, false, null);
                    caseLayouts.Add(layout);
                }
                else
                {
                    var field = GetField(caseType);
                    var layout = new CaseLayout(caseType, $"{i + 1}", field);
                    caseLayouts.Add(layout);
                }
            }

            IReadOnlyList<MemberLayout> CreateMemberLayouts(
                IReadOnlyList<MemberDesc> members,
                DataField? overlappedCaseField,
                int overlappedMemberCount)
            {
                var memberLayouts = new List<MemberLayout>();

                foreach (var member in members)
                {
                    if (member.Type.IsOverlappable && overlappedCaseField != null)
                    {
                        // if only one member is overlappable then specify field for simplicity
                        var field = overlappedMemberCount == 1 ? overlappedCaseField : null;
                        var layout = new MemberLayout(member, field, true, false, null);
                        memberLayouts.Add(layout);
                    }
                    else if (member.Type.IsDecomposible && allowDecomposition)
                    {
                        var nestedLayouts = CreateMemberLayouts(member.Type.Members, overlappedCaseField, overlappedMemberCount);
                        var layout = new MemberLayout(member, null, false, true, nestedLayouts);
                        memberLayouts.Add(layout);
                    }
                    else
                    {
                        var field = GetField(member.Type);
                        var layout = new MemberLayout(member, field, false, false, null);
                        memberLayouts.Add(layout);
                    }
                }

                return memberLayouts;
            }

            TypeDesc GetOverlappedCaseType(IReadOnlyList<MemberDesc> overlappedMembers)
            {
                if (overlappedMembers.Count == 1)
                    return overlappedMembers[0].Type;
                var name = $"({string.Join(", ", overlappedMembers.Select(m => m.Type.TypeName))})";
                return new TypeDesc(name, TypeDescKind.Struct);
            }

            return new UnionLayout(
                union,
                caseLayouts,
                dataFields,
                tagField,
                overlappedField
                );

            DataField GetField(TypeDesc type)
            {
                DataField? field;

                var allowSharing = allowFieldSharing;
                if (type.IsReference && allowReferenceSharing)
                {
                    type = TypeDesc.Object;
                    allowSharing = true;
                }

                if (allowSharing
                    && FindUnusedField(type) is { } foundField)
                {
                    usedFields.Add(foundField);
                    return foundField;
                }

                field = new DataField($"{DataFieldPrefix}{dataFields.Count + 1}", type, canShare: allowSharing);
                usedFields.Add(field);
                dataFields.Add(field);

                return field;

                DataField? FindUnusedField(TypeDesc type)
                {
                    foreach (var f in dataFields)
                    {
                        if (f.CanShare 
                            && f.Type == type
                            && !usedFields.Contains(f))
                        {
                            return f;
                        }
                    }

                    return null;
                }
            }

            bool HasOverlappableMembers(TypeDesc td)
            {
                if (td.IsOverlappable)
                    return true;
                if (td.IsDecomposible && allowDecomposition)
                    return td.Members.Any(m => HasOverlappableMembers(m.Type));
                return false;
            }

            IReadOnlyList<MemberDesc> GetOverlappableMembers(IReadOnlyList<MemberDesc> members)
            {
                var overlapped = new List<MemberDesc>();
                Gather(members);
                return overlapped;

                void Gather(IReadOnlyList<MemberDesc> members)
                {
                    foreach (var member in members)
                    {
                        if (member.Type.IsOverlappable)
                            overlapped.Add(member);
                        if (member.Type.IsDecomposible && allowDecomposition)
                            GetOverlappableMembers(member.Type.Members);
                    }
                }
            }
        }
        #endregion

        #region layout types
        private class UnionLayout
        {
            /// <summary>
            /// The union this layout is based on.
            /// </summary>
            public UnionInfo Union { get; }

            /// <summary>
            /// The layouts for each case.
            /// </summary>
            public IReadOnlyList<CaseLayout> CaseLayouts { get; }

            /// <summary>
            /// All data fields at union level.
            /// </summary>
            public IReadOnlyList<DataField> DataFields { get; }

            /// <summary>
            /// The field for the tag value.
            /// </summary>
            public DataField? TagField { get; }

            /// <summary>
            /// The field for overlapped data.
            /// </summary>
            public DataField? OverlappedField { get; }

            public UnionLayout(
                UnionInfo union, 
                IReadOnlyList<CaseLayout> caseLayouts,
                IReadOnlyList<DataField> caseFields,
                DataField? tagField = null,
                DataField? overlappedField = null)
            {
                this.Union = union;
                this.CaseLayouts = caseLayouts;
                this.DataFields = caseFields;
                this.TagField = tagField;
                this.OverlappedField = overlappedField;
            }
        }

        private class ElementLayout
        {
            /// <summary>
            /// The type of the data element
            /// </summary>
            public TypeDesc Type { get; }

            /// <summary>
            /// The field that stores the data element.
            /// </summary>
            public DataField? Field { get; }

            /// <summary>
            /// If True, the entire element is to be overlapped
            /// </summary>
            public bool IsOverlapped { get; }

            /// <summary>
            /// If True, the entire element is to be decomposed into its members.
            /// </summary>
            public bool IsDecomposed { get; }

            /// <summary>
            /// Any additional decomposed members not part of the overlapped data.
            /// </summary>
            public IReadOnlyList<MemberLayout> Members { get; }

            public ElementLayout(
                TypeDesc type,
                DataField? field,
                bool isOverlapped,
                bool isDecomposed,
                IReadOnlyList<MemberLayout>? members)
            {
                this.Type = type;
                this.Field = field;
                this.IsOverlapped = isOverlapped;
                this.IsDecomposed = isDecomposed;
                this.Members = members ?? Array.Empty<MemberLayout>();
            }

            /// <summary>
            /// All the decomposed members that are part of the overlapped data
            /// </summary>
            public IReadOnlyList<MemberLayout> OverlappedMembers
            {
                get
                {
                    if (_overlappedMembers == null)
                    {
                        _overlappedMembers = this.Members
                            .Where(m => m.IsOverlapped)
                            .ToList();
                    }

                    return _overlappedMembers;
                }
            }

            private IReadOnlyList<MemberLayout>? _overlappedMembers;
        }

        /// <summary>
        /// The layout for a case.
        /// </summary>
        private class CaseLayout : ElementLayout
        {
            public CaseDesc Case { get; }

            /// <summary>
            /// The tag value for the case.
            /// </summary>
            public string TagValue { get; }

            /// <summary>
            /// The field in the overlapped data struct corresponding to this case.
            /// </summary>
            public DataField? OverlappedCaseField { get; } // case field in the overlapped data

            public CaseLayout(
                CaseDesc caseDesc, 
                string tagValue,
                DataField? field,
                DataField? overlappedCaseField,
                bool isDeconstructed,
                IReadOnlyList<MemberLayout>? members)
                : base(caseDesc.Type, field, overlappedCaseField != null, isDeconstructed, members)
            {
                this.Case = caseDesc;
                this.TagValue = tagValue;
                this.OverlappedCaseField = overlappedCaseField;
            }

            public CaseLayout(
                CaseDesc caseDesc,
                string tagValue,
                DataField? field)
                : this(caseDesc, tagValue, field, null, false, Array.Empty<MemberLayout>())
            {
            }
        }

        /// <summary>
        /// The layout for a deconstructable member.
        /// </summary>
        private class MemberLayout : ElementLayout
        {
            public MemberDesc Member { get; }

            public MemberLayout(
                MemberDesc member,
                DataField? field,
                bool isOverlapped,
                bool isDecomposed,
                IReadOnlyList<MemberLayout>? members)
                : base(member.Type, field, isOverlapped, isDecomposed, members)
            {
                this.Member = member;
            }
        }

        /// <summary>
        /// A field that stores data.
        /// </summary>
        private class DataField
        {
            public string Name { get; }
            public TypeDesc Type { get; }
            public DataField? Path { get; }
            public bool CanShare { get; }

            public DataField(string fieldName, TypeDesc type, DataField? path = null, bool canShare = false)
            {
                this.Name = fieldName;
                this.Type = type;
                this.Path = path;
                this.CanShare = canShare;
            }

            public bool IsUnionField => 
                this.Path == null;

            public bool IsOverlappedField =>
                this.Path != null
                && this.Path.IsUnionField
                && this.Path.Name == OverlappedFieldName;

            public bool IsOverlappedCaseField =>
                this.Path != null
                && this.Path.IsOverlappedField;
        }
        #endregion
    }

    #region union declaration types
    public class UnionInfo : IEquatable<UnionInfo>
    {
        /// <summary>
        /// The name used to declare the type (include type parameters if any)
        /// </summary>
        public string DeclarationName { get; }

        /// <summary>
        /// The name w/o type parameters (used for constructor declarations)
        /// </summary>
        public string SimpleName { get; }

        /// <summary>
        /// The namespace the type is declared in.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// The kind of layout to use for the type.
        /// </summary>
        public LayoutOptions Options { get; }

        /// <summary>
        /// The cases
        /// </summary>
        public IReadOnlyList<CaseDesc> Cases { get; }

        public UnionInfo(
            string name,
            LayoutOptions options,
            IReadOnlyList<CaseDesc> cases
            )
        {
            this.DeclarationName = GetDeclarationName(name);
            this.SimpleName = GetSimpleName(name);
            this.Namespace = GetNamespace(name);
            this.Options = options;
            this.Cases = cases;
        }

        public UnionInfo(
            string name,
            IReadOnlyList<CaseDesc> cases
            )
            : this(name, LayoutOptions.Default, cases)
        {
        }

        /// <summary>
        /// Gets name without any dotted path.
        /// </summary>
        private static string GetDeclarationName(string name)
        {
            var lastDot = name.LastIndexOf('.');
            if (lastDot > 0)
                return name.Substring(lastDot + 1);
            return name;
        }

        private static string GetNamespace(string name)
        {
            var lastDot = name.LastIndexOf('.');
            if (lastDot > 0)
                return name.Substring(0, lastDot);
            return "";
        }

        /// <summary>
        /// Gets name without any dotted path or type parameters.
        /// </summary>
        private static string GetSimpleName(string name)
        {
            var declName = GetDeclarationName(name);
            var firstLT = declName.IndexOf('<');
            if (firstLT >= 0)
                return declName.Substring(0, firstLT);
            return declName;
        }

        public bool Equals(UnionInfo other)
        {
            if (!this.DeclarationName.Equals(other.DeclarationName))
                return false;
            if (!this.Namespace.Equals(other.Namespace))
                return false;
            if (this.Options != other.Options)
                return false;
            if (this.Cases.Count != other.Cases.Count)
                return false;
            for (int i = 0; i < this.Cases.Count; i++)
            {
                if (!this.Cases[i].Type.TypeName.Equals(other.Cases[i].Type.TypeName))
                    return false;
            }
            return true;
        }      
    }

    [Flags]
    public enum LayoutOptions
    {
        /// <summary>
        /// Nothing enabled, all cases will have separate fields.
        /// </summary>
        None                        = 0,

        /// <summary>
        /// Allow boxing of value types.
        /// This means all values can be stored in a single field typed as object.
        /// </summary>
        AllowBoxing                 = 1 << 1,

        /// <summary>
        /// Allow sharing of same type fields across cases.
        /// This is only meaningful if deconstruction is also allowed.
        /// </summary>
        AllowFieldSharing           = 1 << 2,

        /// <summary>
        /// Allow sharing of reference type fields across cases.
        /// This means fields for reference type values will be typed as object
        /// and casting will occur during value access.
        /// </summary>
        AllowReferenceFieldSharing  = 1 << 3,

        /// <summary>
        /// Allow decomposition of values into elements stored in separate fields
        /// </summary>
        AllowDecomposition         = 1 << 4,

        /// <summary>
        /// Allow overlapping fields from seperate cases
        /// </summary>
        AllowOverlappingFields      = 1 << 5,

        Default = AllowFieldSharing | AllowReferenceFieldSharing | AllowDecomposition | AllowOverlappingFields,
    }

    public class CaseDesc
    {
        public TypeDesc Type { get; }
        public bool GenerateType { get; }

        public CaseDesc(
            TypeDesc type,
            bool generateType = false)
        {
            this.Type = type;
            this.GenerateType = generateType;
        }

        public static implicit operator CaseDesc(TypeDesc type) =>
            new CaseDesc(type, false);
    }

    public class TypeDesc
    {
        /// <summary>
        /// The full type name of the type
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// The kind of type (primitive, class, struct, etc.)
        /// </summary>
        public TypeDescKind Kind { get; }

        /// <summary>
        /// The decomposible members of the type, if any.
        /// </summary>
        public IReadOnlyList<MemberDesc> Members { get; }

        /// <summary>
        /// The non-nullable form of the <see cref="TypeDesc"/>
        /// </summary>
        private readonly TypeDesc? _nonNullable;

        private TypeDesc(
            string typeName, 
            TypeDescKind kind, 
            IReadOnlyList<MemberDesc>? members, 
            TypeDesc? nonNullable)
        {
            this.TypeName = typeName;
            this.Kind = kind;
            this.Members = members ?? Array.Empty<MemberDesc>();
            _nonNullable = nonNullable;
        }

        /// <summary>
        /// Inclusion of members implies it is decomposable.
        /// </summary>
        public TypeDesc(string typeName, IReadOnlyList<MemberDesc> members)
            : this(typeName, TypeDescKind.DecomposableStruct, members, null)
        {
            if (members.Count < 1)
                throw new ArgumentException("Decomposable struct must have at least one member", nameof(members));
        }

        public TypeDesc(string typeName, TypeDescKind kind)
            : this(typeName, kind, null, null)
        {
            if (kind == TypeDescKind.DecomposableStruct)
                throw new ArgumentException("Decomposable struct must specify members", nameof(kind));
        }

        public bool IsReference =>
            this.Kind switch
            {
                TypeDescKind.Class => true,
                TypeDescKind.Interface => true,
                _ => false,
            };

        public bool IsOverlappable =>
            this.Kind switch
            {
                TypeDescKind.Primitive => true,
                TypeDescKind.OverlappableStruct => true,
                _ => false
            };

        public bool IsDecomposible =>
            this.Kind == TypeDescKind.DecomposableStruct;

        public bool IsNullable => 
            this.Nullable == this;

        public bool MightBeNullable =>
            this.IsNullable
            || this.Kind == TypeDescKind.TypeParameter;

        public TypeDesc NonNullable =>
            _nonNullable ?? this;

        private TypeDesc? _nullable;
        public TypeDesc Nullable
        {
            get
            {
                if (_nullable == null)
                {
                    if (!this.TypeName.EndsWith("?"))
                    {
                        _nullable = new TypeDesc(this.TypeName + "?", this.Kind, this.Members, this);
                    }
                    else
                    {
                        _nullable = this;
                    }
                }

                return _nullable;
            }
        }

        public static TypeDesc Byte = new TypeDesc("byte", TypeDescKind.Primitive);
        public static TypeDesc Int32 = new TypeDesc("int", TypeDescKind.Primitive);
        public static TypeDesc Int64 = new TypeDesc("long", TypeDescKind.Primitive);
        public static TypeDesc Float = new TypeDesc("float", TypeDescKind.Primitive);
        public static TypeDesc Double = new TypeDesc("double", TypeDescKind.Primitive);
        public static TypeDesc Decimal = new TypeDesc("decimal", TypeDescKind.Primitive);
        public static TypeDesc String = new TypeDesc("string", TypeDescKind.Class);
        public static TypeDesc Object = new TypeDesc("object", TypeDescKind.Class);
    }

    public enum TypeDescKind
    {
        /// <summary>
        /// Overlappable primitive
        /// </summary>
        Primitive,

        /// <summary>
        /// Any class or class constrained type parameter
        /// </summary>
        Class,

        /// <summary>
        /// Any interface
        /// </summary>
        Interface,

        /// <summary>
        /// Any struct not known to be overlappable
        /// </summary>
        Struct,

        /// <summary>
        /// Overlappable struct (does not contain reference type values)
        /// </summary>
        OverlappableStruct,

        /// <summary>
        /// Decomposable struct (record struct with deconstructor or public settable properties)
        /// </summary>
        DecomposableStruct,

        /// <summary>
        /// Unconstrained type parameter
        /// </summary>
        TypeParameter,
    }

    /// <summary>
    /// A decomposable member
    /// </summary>
    public class MemberDesc
    {
        public string Name { get; }

        public TypeDesc Type { get; }

        /// <summary>
        /// True if the member is a parameter to the constructor & deconstructor, false if it is a property.
        /// </summary>
        public bool IsParameter { get; }

        public MemberDesc(string name, TypeDesc type, bool isParameter = false)
        {
            Name = name;
            Type = type;
            IsParameter = isParameter;
        }

        public MemberDesc(TypeDesc type)
            : this("", type, isParameter: true)
        {
        }

        public static implicit operator MemberDesc(TypeDesc type) =>
            new MemberDesc(type);
    }
    #endregion

#if !T4
}
#endif
// #>