// <#+
#if !T4
namespace UnionTypes.Toolkit.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#endif

#nullable enable

    public class CustomUnionGenerator
    {
        private readonly CodeWriter _writer = new CodeWriter();

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
            _writer.WriteLine("using System;");
            _writer.WriteLine("using System.Collections.Generic;");
            _writer.WriteLine("using System.Diagnostics.CodeAnalysis;");
            _writer.WriteLine("using System.Runtime.CompilerServices;");
            _writer.WriteLine("using System.Runtime.InteropServices;");
            _writer.WriteLine("#nullable enable");
            _writer.WriteLine("#pragma warning disable CS8600");
            _writer.WriteLine("#pragma warning disable CS8601");
            _writer.WriteLine("#pragma warning disable CS8603");
            _writer.WriteLine("#pragma warning disable CS8604");
            _writer.WriteLine("#pragma warning disable CS8605");
            _writer.WriteLine("#pragma warning disable CS8618");
            _writer.WriteLine();
            WriteUnionType(union);
            return _writer.WrittenText;
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
                _writer.WriteLine($"namespace {union.Namespace}");
                _writer.WriteBraceNested(() =>
                {
                    WriteUnion();
                });
            }
            else
            {
                WriteUnion();
            }

            void WriteUnion()
            {
                _writer.WriteLine("[System.Runtime.CompilerServices.Union]");
                _writer.WriteLine($"{union.Accessibility} partial struct {union.DeclarationName} : System.Runtime.CompilerServices.IUnion");
                _writer.WriteBraceNested(() =>
                {
                    _writer.WriteLineSeparatedBlocks(() =>
                    {
                        _writer.WriteBlock(() => WriteStorageFields(layout));
                        _writer.WriteBlock(() => WriteOverlappedTypeDeclaration(layout));
                        _writer.WriteBlock(() => WriteGeneratedCaseTypeDeclarations(layout));
                        _writer.WriteBlock(() => WriteCaseConstructors(layout));
                        _writer.WriteBlock(() => WriteCaseAccessors(layout));
                        _writer.WriteBlock(() => WriteValueProperty(layout));
                        _writer.WriteBlock(() => WriteHasValueProperty(layout));
                        _writer.WriteBlock(() => WriteTryGetValueMethods(layout));
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
                _writer.WriteLine($"private readonly {layout.TagField.Type.TypeName} {layout.TagField.Name};");

            if (layout.OverlappedField != null)
                _writer.WriteLine($"private readonly {layout.OverlappedField.Type.TypeName} {layout.OverlappedField.Name};");

            foreach (var field in layout.DataFields)
            {
                _writer.WriteLine($"private readonly {field.Type.TypeName} {field.Name};");
            }
        }

        /// <summary>
        /// Writes the declaration of the type that holds the overlapped elements of each case, if at least two cases have overlappable elements.
        /// </summary>
        private void WriteOverlappedTypeDeclaration(UnionLayout layout)
        {
            if (layout.OverlappedField != null)
            {
                _writer.WriteLine($"[StructLayout(LayoutKind.Explicit)]");
                _writer.WriteLine($"private struct {OverlappedTypeName}");
                _writer.WriteBraceNested(() =>
                {
                    foreach (var caseLayout in layout.CaseLayouts)
                    {
                        if (caseLayout.OverlappedCaseField != null)
                        {
                            _writer.WriteLine($"[FieldOffset(0)] public {caseLayout.OverlappedCaseField.Type.TypeName} {caseLayout.OverlappedCaseField.Name};");
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Writes a declaration for any case types that require generation as a record struct.
        /// </summary>
        private void WriteGeneratedCaseTypeDeclarations(UnionLayout layout)
        {
            foreach (var caseInfo in layout.Union.Cases)
            {
                if (caseInfo.GenerateType)
                {
                    var members = string.Join(", ", caseInfo.Type.Members.Select(m => $"{m.Type.TypeName} {m.Name}"));
                    _writer.WriteLine($"public record struct {caseInfo.Type.TypeName}({members});");
                }
            }
        }

        /// <summary>
        /// Writes public constructors for all the case types.
        /// </summary>
        private void WriteCaseConstructors(UnionLayout layout)
        {
            _writer.WriteLineSeparatedBlocks(() =>
            {
                foreach (var caseLayout in layout.CaseLayouts)
                {
                    _writer.WriteBlock(() => WriteConstructor(caseLayout));
                }
            });

            // The constructor assigns the value to its corresponding field, 
            // or decomposes it into its members and assigns those members to their corresponding fields.
            void WriteConstructor(CaseLayout caseLayout)
            {
                var caseType = caseLayout.Case.Type;
                _writer.WriteLine($"public {layout.Union.SimpleName}({caseType.TypeName} value)");
                _writer.WriteBraceNested(() =>
                {
                    if (caseType.IsReference
                        || caseType.MightBeNullable)
                    {
                        // null values become equivalent of default for the struct
                        _writer.WriteLine("if (value is {} v)");
                        _writer.WriteBraceNested(() =>
                        {
                            WriteBody("v");                           
                        });
                    }
                    else 
                    {
                        WriteBody("value");
                    }

                    void WriteBody(string valueName)
                    {                       
                        if (layout.TagField != null)
                        {
                            _writer.WriteLine($"{layout.TagField.Name} = {caseLayout.TagValue};");
                        }

                        if (caseLayout.IsDecomposed)
                        {
                            Decompose(caseLayout, valueName);
                        }
                        else if (caseLayout.Field != null)
                        {
                            WriteFieldReference(caseLayout.Field);
                            _writer.WriteLine($" = {valueName};");
                        }
                        else
                        {
                            throw new InvalidOperationException("Unexpected layout writing constructor");
                        }
                    }
                });

                void Decompose(ElementLayout elementLayout, string valueName)
                {
                    var locals = new List<LocalMember>();

                    DecomposeElement(caseLayout, valueName);

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
                            _writer.WriteLine($" = {tuple};");
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
                            _writer.Write("(");

                            for (int i = 0; i < parameterMembers.Count; i++)
                            {
                                var member = parameterMembers[i];
                                if (i > 0)
                                    _writer.Write(", ");

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
                                    _writer.Write($"var {localName}");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected layout writing constructor");
                                }
                            }

                            _writer.WriteLine($") = {source};");
                        }
                        else if (parameterMembers.Count == 1)
                        {
                            // if only one parameter, cannot use deconstruction syntax, so call method directly
                            var member = parameterMembers[0];
                            _writer.WriteLine($"{source}.Deconstruct(out var v);");

                            if (member.Field != null)
                            {
                                WriteFieldReference(member.Field);
                            }
                            else
                            {
                                var localName = $"{LocalVariablePrefix}{locals.Count}";
                                var local = new LocalMember(localName, member);
                                locals.Add(local);
                                _writer.Write(localName);
                            }

                            _writer.WriteLine(" = v;");
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
                                    _writer.Write(" = ");
                                    _writer.Write($"{source}.{member.Member.Name}");
                                    _writer.WriteLine(";");
                                }
                                else if (member.IsDecomposed || member.IsOverlapped)
                                {
                                    // assign to variable and handle later
                                    var localName = $"{LocalVariablePrefix}{locals.Count}";
                                    var local = new LocalMember(localName, member);
                                    locals.Add(local);
                                    _writer.Write($"var {localName} = {source}.{member.Member.Name};");
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

            _writer.WriteLineSeparatedBlocks(() =>
            {
                for (int i = 0; i < layout.CaseLayouts.Count; i++)
                {
                    var caseLayout = layout.CaseLayouts[i];
                    _writer.WriteBlock(() => WriteCaseAccessor(caseLayout));
                }
            });

            void WriteCaseAccessor(CaseLayout caseLayout)
            {
                var locals = new List<LocalMember>();
                GatherLocals(caseLayout, locals);
                var map = locals.ToDictionary(loc => loc.Member);

                if (locals.Count > 0)
                {
                    _writer.WriteLine($"private {caseLayout.StorageType.TypeName} {AccessorNamePrefix}{caseLayout.TagValue}()");
                    _writer.WriteBraceNested(() =>
                    {
                        // multi-element overlapped data into locals
                        if (caseLayout.OverlappedCaseField != null)
                        {
                            var deconstruct = locals.Count > 1 
                                ? $"({string.Join(", ", locals.Select(loc => $"var {loc.LocalName}"))})"
                                : $"var {locals[0].LocalName}";
                            _writer.Write($"{deconstruct} = ");
                            WriteFieldReference(caseLayout.OverlappedCaseField);
                            _writer.WriteLine(";");
                        }

                        _writer.Write("return ");
                        AccessElement(caseLayout, map);
                        _writer.WriteLine(";");
                    });
                }
                else
                {
                    _writer.Write($"private {caseLayout.StorageType.TypeName} {AccessorNamePrefix}{caseLayout.TagValue}() => ");
                    AccessElement(caseLayout, map);
                    _writer.WriteLine(";");
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
                    _writer.Write(local.LocalName);
                }
                else if (elementLayout.IsDecomposed)
                {
                    Recompose(elementLayout, localsMap);
                }
                else if (elementLayout.Field != null)
                {
                    if (!elementLayout.Field.Type.Equals(elementLayout.StorageType))
                        _writer.Write($"({elementLayout.StorageType.TypeName})");

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

                _writer.Write("new (");
                
                for (int i = 0; i < parameterMembers.Count; i++)
                {
                    if (i > 0)
                        _writer.Write(", ");

                    var memberLayout = parameterMembers[i];
                    AccessElement(memberLayout, localsMap);
                }

                _writer.Write(")");                       

                if (propertyMembers.Count > 0)
                {
                    _writer.Write(" { ");
                    for (int i = 0; i < propertyMembers.Count; i++)
                    {
                        if (i > 0)
                            _writer.Write(", ");

                        var memberLayout = propertyMembers[i];
                        _writer.Write($"{memberLayout.Member.Name} = ");
                        AccessElement(memberLayout, localsMap);
                    }

                    _writer.Write(" }");
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
                _writer.Write(".");
            }
            _writer.Write(field.Name);
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
                return $"this.{AccessorNamePrefix}{caseLayout.TagValue}()";
            }
        }

        /// <summary>
        /// Generates the union's Value property
        /// </summary>
        private void WriteValueProperty(UnionLayout layout)
        {
            _writer.WriteLine($"public object? Value =>");
            _writer.WriteNested(() =>
            {
                if (layout.TagField != null)
                {
                    // the type has a tag field, so use a switch expression to access/reconstruct the appropriate case value.`
                    _writer.WriteLine($"{layout.TagField.Name} switch");
                    _writer.WriteLine("{");
                    _writer.WriteNested(() =>
                    {
                        foreach (var caseLayout in layout.CaseLayouts)
                        {
                            var caseValue = GetCaseAccessExpression(layout, caseLayout);
                            _writer.WriteLine($"{caseLayout.TagValue} => {caseValue},");
                        }
                        _writer.WriteLine("_ => null");
                    });
                    _writer.WriteLine("};");
                }
                else if (layout.DataFields.Count == 1)
                {
                    // the value is only ever stored in exactly one field (boxed layout) so just return that field value.
                    WriteFieldReference(layout.DataFields[0]);
                    _writer.WriteLine(";");
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
        private void WriteHasValueProperty(UnionLayout layout)
        {
            if (layout.TagField != null)
            {
                _writer.Write($"public bool HasValue => ");
                WriteFieldReference(layout.TagField);
                _writer.WriteLine(" != 0;");
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

            _writer.WriteLineSeparatedBlocks(() =>
            {
                foreach (var caseLayout in layout.CaseLayouts)
                {
                    _writer.WriteBlock(() => WriteCaseTypeGetValue(caseLayout));
                }
            });

            void WriteCaseTypeGetValue(CaseLayout caseLayout)
            {
                _writer.WriteLine($"{caseLayout.Case.Accessibility} bool TryGetValue([NotNullWhen(true)] out {caseLayout.Case.Type.TypeName} value)");
                _writer.WriteBraceNested(() =>
                {
                    if (caseLayout.Case.NonDisjointCases.Count > 0)
                    {
                        // the case type is not entirely distinct from other case's types, 
                        // so we need to check values encoded as other cases to see if they match this case's type.
                        _writer.WriteLine("switch (_kind)");
                        _writer.WriteBraceNested(() =>
                        {
                            for (int i = 0; i < layout.CaseLayouts.Count; i++)
                            {
                                var otherCaseLayout = layout.CaseLayouts[i];
                                var caseAccessExpr = GetCaseAccessExpression(layout, otherCaseLayout);
                                if (otherCaseLayout.TagValue == caseLayout.TagValue)
                                {
                                    // this is my own case, so just return the value
                                    _writer.WriteLine($"case {otherCaseLayout.TagValue}:");
                                    _writer.WriteLineNested($"value = {caseAccessExpr};");
                                    if (caseLayout.Case.Type.MightBeNullable)
                                        _writer.WriteLineNested("return value is not null;");
                                    else 
                                        _writer.WriteLineNested("return true;");
                                }
                                else if (caseLayout.Case.NonDisjointCases.Contains(i))
                                {
                                    // this is a different case, so test it against this case's type and return the value if it matches
                                    _writer.WriteLine($"case {otherCaseLayout.TagValue} when {caseAccessExpr} is {caseLayout.StorageType.TypeName} v:");
                                    _writer.WriteLineNested($"value = v;");
                                    if (caseLayout.Case.Type.MightBeNullable)
                                        _writer.WriteLineNested("return value is not null;");
                                    else 
                                        _writer.WriteLineNested("return true;");
                                }
                            }
                            _writer.WriteLine("default:");
                            _writer.WriteLineNested("value = default!;");
                            _writer.WriteLineNested("return false;");
                        });
                    }
                    else
                    {
                        _writer.WriteLine($"if ({layout.TagField.Name} == {caseLayout.TagValue})");
                        _writer.WriteBraceNested(() =>
                        {
                            var caseValue = GetCaseAccessExpression(layout, caseLayout);
                            _writer.WriteLine($"value = {caseValue};");
                            _writer.WriteLine("return true;");
                        });
                        _writer.WriteLine("else");
                        _writer.WriteBraceNested(() =>
                        {
                            _writer.WriteLine("value = default!;");
                            _writer.WriteLine("return false;");
                        });                       
                    }
                });
            }
        }

        #region layout

        /// <summary>
        /// Computes the layout information for the unios based on options and cases
        /// </summary>
        private UnionLayout ComputeLayout(UnionInfo union)
        {
            if (union.Style == LayoutStyle.Boxed)
                return ComputeBoxedLayout(union);
            return ComputeTaggedLayout(union);
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
        /// <param name="allowDecomposition">Indicates whether cases can be decomposed into their constituent elements.</param>
        /// <param name="allowOverlap">Indicates whether overlapping of overlappable fields is allowed.</param>
        /// <returns>The computed layout for the union.</returns>
        private UnionLayout ComputeTaggedLayout(
            UnionInfo union)
        {
            var tagField = new DataField(TagFieldName, TypeDesc.Int32);
            var dataFields = new List<DataField>();

            // if at least two cases have some overlapping members then create a field for the overlapped data
            DataField? overlappedField = null;
            if (union.Cases.Count(c => HasOverlappableMembers(c.Type)) >= 2)
            {
                overlappedField = new DataField(OverlappedFieldName, new TypeDesc(OverlappedTypeName, TypeDescKind.Struct));
            }

            var caseLayouts = new List<CaseLayout>();
            var usedFields = new HashSet<DataField>();

            for (int i = 0; i < union.Cases.Count; i++)
            {
                var caseDesc = union.Cases[i];
                var caseType = caseDesc.Type.NonNullable;    // use the non-nullable type for storage
                usedFields.Clear();

                var caseTag = $"{i + 1}";

                switch (caseDesc.Type.Storage)
                {
                    case StorageKind.Box:
                        caseLayouts.Add(GetBoxedLayout(caseDesc, caseType, caseTag));
                        continue;                  
                    case StorageKind.Decompose:
                        caseLayouts.Add(GetDecomposedLayout(caseDesc, caseType, caseTag));
                        continue;
                    case StorageKind.Isolate:
                        caseLayouts.Add(GetIsolatedLayout(caseDesc, caseType, caseTag));
                        continue;
                    case StorageKind.Overlap:
                        if (overlappedField != null)
                        {
                            caseLayouts.Add(GetOverlappedLayout(caseDesc, caseType, caseTag));                            
                        }
                        else if (caseDesc.Type.Members.Count > 0)
                        {
                            // cannot overlap if only one case has overlappable elements, so decompose the case instead (if it has members)
                            caseLayouts.Add(GetDecomposedLayout(caseDesc, caseType, caseTag));
                        }
                        else
                        {
                            // otherwise isolate
                            caseLayouts.Add(GetIsolatedLayout(caseDesc, caseType, caseTag));                            
                        }
                        break;
                }
            }

            CaseLayout GetBoxedLayout(CaseDesc caseDesc, TypeDesc caseType, string tagValue)
            {
                var field = GetField(caseType, allowBoxing: true);
                return new CaseLayout(caseDesc, tagValue, field);
            }

            CaseLayout GetIsolatedLayout(CaseDesc caseDesc, TypeDesc caseType, string tagValue)
            {
                var field = GetField(caseType, allowBoxing: false);
                return new CaseLayout(caseDesc, tagValue, field);
            }

            CaseLayout GetOverlappedLayout(CaseDesc caseDesc, TypeDesc caseType, string tagValue)
            {
                var overlappedCaseField = new DataField($"{OverlappedCaseFieldName}{tagValue}", caseType, overlappedField);
                return new CaseLayout(caseDesc, tagValue, overlappedCaseField, overlappedCaseField, false, null);
            }

            CaseLayout GetDecomposedLayout(CaseDesc caseDesc, TypeDesc caseType, string tagValue)
            {
                var overlappableMembers = GetOverlappableMembers(caseType.Members);
                DataField? overlappedCaseField = null;
                if (overlappedField != null
                    && overlappableMembers.Count > 0)
                {
                    var fieldType = GetOverlappedCaseType(overlappableMembers);
                    overlappedCaseField = new DataField($"{OverlappedCaseFieldName}{tagValue}", fieldType, overlappedField);
                }
                var memberLayouts = CreateMemberLayouts(caseType.Members, overlappedCaseField, overlappableMembers.Count);
                return new CaseLayout(caseDesc, tagValue, null, overlappedCaseField, true, memberLayouts);
            }

            IReadOnlyList<MemberLayout> CreateMemberLayouts(
                IReadOnlyList<MemberDesc> members,
                DataField? overlappedCaseField,
                int overlappedMemberCount)
            {
                var memberLayouts = new List<MemberLayout>();

                foreach (var member in members)
                {
                    switch (member.Type.Storage)
                    {
                        case StorageKind.Box:
                            var field = GetField(member.Type, allowBoxing: true);
                            var layout = new MemberLayout(member, field, false, false, null);
                            memberLayouts.Add(layout);                        
                            break;
                        case StorageKind.Decompose:
                            var nestedLayouts = CreateMemberLayouts(member.Type.Members, overlappedCaseField, overlappedMemberCount);
                            layout = new MemberLayout(member, null, false, true, nestedLayouts);
                            memberLayouts.Add(layout);
                            break;
                        case StorageKind.Isolate:
                            field = GetField(member.Type, allowBoxing: false);
                            layout = new MemberLayout(member, field, false, false, null);
                            memberLayouts.Add(layout);
                            break;
                        case StorageKind.Overlap:
                            if (overlappedCaseField != null)
                            {
                                // if only one member of this case is overlappable then specify field for simplicity
                                field = overlappedMemberCount == 1 ? overlappedCaseField : null;
                                layout = new MemberLayout(member, field, true, false, null);
                                memberLayouts.Add(layout);
                            }
                            else if (member.Type.Members.Count > 0)
                            {
                                // cannot overlap if only one case has overlappable elements, so decompose the member instead
                                nestedLayouts = CreateMemberLayouts(member.Type.Members, null, 0);
                                layout = new MemberLayout(member, null, false, true, nestedLayouts);
                                memberLayouts.Add(layout);
                            }
                            else
                            {
                                // otherwise, isolate the member
                                field = GetField(member.Type, allowBoxing: false);
                                layout = new MemberLayout(member, field, false, false, null);
                                memberLayouts.Add(layout);
                            }
                            break;
                    }
                }

                return memberLayouts;
            }

            TypeDesc GetOverlappedCaseType(IReadOnlyList<MemberDesc> overlappedMembers)
            {
                // if more than one member return a tuple type for the overlapped case data
                if (overlappedMembers.Count == 1)
                    return overlappedMembers[0].Type;
                var name = $"({string.Join(", ", overlappedMembers.Select(m => m.Type.TypeName))})";
                return new TypeDesc(name, TypeDescKind.Struct, StorageKind.Overlap);
            }

            return new UnionLayout(
                union,
                caseLayouts,
                dataFields,
                tagField,
                overlappedField
                );

            DataField GetField(TypeDesc type, bool allowBoxing)
            {
                DataField? field;

                if (allowBoxing)
                {
                    // use object as type for maximal field sharing
                    type = TypeDesc.Object.Nullable;
                }

                // look for data field of this type not yet used by this case
                if (FindUnusedField(type) is { } foundField)
                {
                    usedFields.Add(foundField);
                    return foundField;
                }

                // create a new field for this type
                field = new DataField($"{DataFieldPrefix}{dataFields.Count + 1}", type, canShare: true);
                usedFields.Add(field);
                dataFields.Add(field);

                return field;

                DataField? FindUnusedField(TypeDesc type)
                {
                    foreach (var f in dataFields)
                    {
                        if (f.CanShare
                            && f.Type.Equals(type)
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
                if (td.Storage == StorageKind.Overlap)
                    return true;
                if (td.Storage == StorageKind.Decompose)
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
                        if (member.Type.Storage == StorageKind.Overlap)
                        {
                            overlapped.Add(member);
                        }
                        else if (member.Type.Storage == StorageKind.Decompose)
                        {
                            Gather(member.Type.Members);
                        }
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
            public TypeDesc StorageType { get; }

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
                this.StorageType = type;
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
                : base(caseDesc.Type.NonNullable, field, overlappedCaseField != null, isDeconstructed, members)
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
        /// The accessiblity of the type (public, internal, etc.)
        /// </summary>
        public string Accessibility { get; }

        /// <summary>
        /// The kind of layout to use for the type.
        /// </summary>
        public LayoutStyle Style { get; }

        /// <summary>
        /// The cases
        /// </summary>
        public IReadOnlyList<CaseDesc> Cases { get; }

        public UnionInfo(
            string name,
            IReadOnlyList<CaseDesc> cases,
            LayoutStyle style = LayoutStyle.Tagged,
            string accessibility = "public"
            )
        {
            this.DeclarationName = GetDeclarationName(name);
            this.SimpleName = GetSimpleName(name);
            this.Namespace = GetNamespace(name);
            this.Style = style;
            this.Cases = cases;
            this.Accessibility = accessibility;
        }

        /// <summary>
        /// Gets name without any dotted path.
        /// </summary>
        private static string GetDeclarationName(string name)
        {
            // remove global prefix is present
            if (name.StartsWith("global::"))
                name = name.Substring("global::".Length);

            // remove namespace if present
            var lastDot = name.LastIndexOf('.');
            if (lastDot > 0)
                return name.Substring(lastDot + 1);

            return name;
        }

        private static string GetNamespace(string name)
        {
            // remove global prefix is present
            if (name.StartsWith("global::"))
                name = name.Substring("global::".Length);

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

            // remove type parameters if present
            var firstLT = declName.IndexOf('<');
            if (firstLT >= 0)
                return declName.Substring(0, firstLT);

            return declName;
        }

        public bool Equals(UnionInfo other)
        {
            if (this.DeclarationName.Equals(other.DeclarationName)
                && this.Namespace.Equals(other.Namespace)
                && this.Style == other.Style
                && this.Accessibility == other.Accessibility
                && this.Cases.Count != other.Cases.Count)
            {
                for (int i = 0; i < this.Cases.Count; i++)
                {
                    if (!this.Cases[i].Type.TypeName.Equals(other.Cases[i].Type.TypeName))
                        return false;
                }
                return true;
            }
            return false;
        }      

        public override bool Equals(object obj) =>
            obj is UnionInfo other && this.Equals(other);

        public override int GetHashCode()
        {
            return this.DeclarationName.GetHashCode();
        }
    }

    [Flags]
    public enum LayoutStyle
    {
        /// <summary>
        /// The layout uses a single object field to store all values (boxed layout). This is the default layout style.
        /// </summary>
        Boxed,

        /// <summary>
        /// The layout uses a tag field and an optimal layout using overlapped or shared fields across cases as appropriate.
        /// </summary>
        Tagged = 1
    }

    public class CaseDesc : IEquatable<CaseDesc>
    {
        /// <summary>
        /// The type of the case.
        /// </summary>
        public TypeDesc Type { get; }

        /// <summary>
        /// The indices of other cases that not provably disjoint from this case.
        /// In other words, this case may hold a value that could also be held by the other cases in the list.
        /// </summary>
        public IReadOnlyList<int> NonDisjointCases { get; }

        public string Accessibility { get; }

        /// <summary>
        /// If true, a record struct type for the case will be generated from the members as a nested type within the union type.
        /// </summary>
        public bool GenerateType { get; }

        private CaseDesc(
            TypeDesc type,
            IReadOnlyList<int>? nonDisjointCases,
            bool generateType,
            string accessibility)
        {
            this.Type = type;
            this.NonDisjointCases = nonDisjointCases ?? Array.Empty<int>();
            this.GenerateType = generateType;
            this.Accessibility = accessibility;
        }

        public CaseDesc(TypeDesc type, bool generateType, string accessibility = "public")
            : this(type, null, generateType, accessibility)
        {
        }

        public CaseDesc(TypeDesc type, IReadOnlyList<int>? nonDisjointCases = null, string accessibility = "public")
            : this(type, nonDisjointCases, false, accessibility)
        {
        }

        public bool Equals(CaseDesc other)
        {
            if (this.Type.Equals(other.Type)
                && this.GenerateType == other.GenerateType
                && this.Accessibility == other.Accessibility)
            {
                if (this.NonDisjointCases.Count != other.NonDisjointCases.Count)
                    return false;
                for (int i = 0; i < this.NonDisjointCases.Count; i++)
                {
                    if (this.NonDisjointCases[i] != other.NonDisjointCases[i])
                        return false;
                }
                return true;
            }
            return false;
        }

        public override bool Equals(object obj) =>
            obj is CaseDesc other && this.Equals(other);

        public override int GetHashCode()
        {
            return this.Type.GetHashCode();
        }
    }


    public class TypeDesc : IEquatable<TypeDesc>
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
        /// The <see cref="StorageKind"/> requested for the type.
        /// </summary>
        public StorageKind Storage { get; }

        /// <summary>
        /// The decomposible members of the type, if any.
        /// </summary>
        public IReadOnlyList<MemberDesc> Members { get; }

        /// <summary>
        /// The non-nullable variant of this type (if this type is nullable)
        /// </summary>
        public TypeDesc NonNullable { get; }

        /// <summary>
        /// The nullable variant of this type (if this type is non-nullable)
        /// </summary>
        public TypeDesc Nullable { get; }

        private TypeDesc(
            string typeName, 
            TypeDescKind kind, 
            StorageKind storage,    
            IReadOnlyList<MemberDesc>? members, 
            TypeDesc? nullable,
            TypeDesc? nonNullable)
        {
            this.TypeName = typeName;
            this.Kind = kind;
            this.Members = members ?? Array.Empty<MemberDesc>();
            this.Storage = storage == StorageKind.None ? GetDefaultStorageKind(this) : storage;

            if (nullable == null)
            {
                if (IsNullableType(typeName))
                    this.Nullable = this; // i'm the nullable type
                else
                    this.Nullable = new TypeDesc(typeName + "?", kind, StorageKind.None, members, null, this);
            }
            else
            {
                this.Nullable = nullable;                
            }

            if (nonNullable == null)
            {
                if (IsNullableType(typeName))
                    this.NonNullable = new TypeDesc(typeName.Substring(0, typeName.Length - 1), kind, StorageKind.None, members, this, null);
                else
                    this.NonNullable = this; // i'm the non-nullable type
            }
            else
            {
                this.NonNullable = nonNullable;
            }
        }

        private static StorageKind GetDefaultStorageKind(TypeDesc type) =>
            type.Kind switch
            {
                TypeDescKind.Primitive => StorageKind.Overlap,
                TypeDescKind.Class => StorageKind.Box,
                TypeDescKind.Interface => StorageKind.Box,
                TypeDescKind.Struct => StorageKind.Isolate,
                TypeDescKind.UnconstrainedTypeParameter => StorageKind.Isolate,
                TypeDescKind.ClassTypeParameter => StorageKind.Box,
                TypeDescKind.StructTypeParameter => StorageKind.Isolate,
                _ => StorageKind.Isolate
            };

        private static bool IsNullableType(string typeName) => typeName.EndsWith("?");

        /// <summary>
        /// Construct a <see cref="TypeDesc"/> that has no members.
        /// </summary>
        public TypeDesc(string typeName, TypeDescKind kind, StorageKind storage = StorageKind.None)
            : this(typeName, kind, storage, null, null, null)
        {
        }

        /// <summary>
        /// Construct a <see cref="TypeDesc"/> that is a decomposable struct.
        /// </summary>
        public TypeDesc(string typeName, IReadOnlyList<MemberDesc> members)
            : this(typeName, TypeDescKind.Struct, StorageKind.Decompose, members, null, null)
        {
        }

        /// <summary>
        /// Construct a <see cref="TypeDesc"/> that is a fully described.
        /// </summary>
        public TypeDesc(string typeName, TypeDescKind kind, StorageKind storage, IReadOnlyList<MemberDesc>? members = null)
            : this(typeName, kind, storage, members, null, null)
        {
        }

        /// <summary>
        /// True if the type is a reference type (class, interface, or class constrained type parameter)
        /// </summary>
        public bool IsReference =>
            this.Kind switch
            {
                TypeDescKind.Class => true,
                TypeDescKind.Interface => true,
                TypeDescKind.ClassTypeParameter => true,
                _ => false,
            };

        /// <summary>
        /// True if the type is a value type (primitive, struct, or struct constrained type parameter)
        /// </summary>
        public bool IsValueType =>
            this.Kind switch
            {
                TypeDescKind.Primitive => true,
                TypeDescKind.Struct => true,
                TypeDescKind.StructTypeParameter => true,
                _ => false
            };

        /// <summary>
        /// True if the type is a type parameter (unconstrained, struct constrained, or class constrained)
        /// </summary>
        public bool IsTypeParameter =>
            this.Kind switch
            {
                TypeDescKind.UnconstrainedTypeParameter => true,
                TypeDescKind.StructTypeParameter => true,
                TypeDescKind.ClassTypeParameter => true,
                _ => false
            };

        /// <summary>
        /// True if the type can be assigned null
        /// </summary>
        public bool IsNullable => 
            this.Nullable == this;

        /// <summary>
        /// True if the type might be nullable (either is nullable or is an unconstrained type parameter)
        /// </summary>
        public bool MightBeNullable =>
            this.IsNullable
            || this.Kind == TypeDescKind.ClassTypeParameter
            || this.Kind == TypeDescKind.UnconstrainedTypeParameter;


        public bool Equals(TypeDesc other)
        {
            if (this.Kind != other.Kind
                || this.Storage != other.Storage
                || this.TypeName != other.TypeName)
                return false;

            if (this.Members.Count != other.Members.Count)
                return false;

            for (int i = 0; i < this.Members.Count; i++)
            {
                if (!this.Members[i].Equals(other.Members[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) =>
            obj is TypeDesc other && this.Equals(other);

        public override int GetHashCode()
        {
            return this.TypeName.GetHashCode();
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

    /// <summary>
    /// The kind of storage to use for a case or member.
    /// </summary>
    public enum StorageKind
    {
        /// <summary>
        /// No kind is specified, the default storage kind should be used based on the type.
        /// (primitives=overlapped, reference types=boxed, structs = isolated)
        /// </summary>
        None,
        
        /// <summary>
        /// The value should be boxed and stored as a reference type in sharable object field.
        /// </summary>
        Box,
        
        /// <summary>
        /// The value should be decomposed into its constituent member values before storing.
        /// </summary>
        Decompose,

        /// <summary>
        /// The value should be stored in a separate strongly typed field.
        /// </summary>
        Isolate,
        
        /// <summary>
        /// The value should be stored in the overlapped field for the corresponding case, when available.
        /// </summary>
        Overlap
    }


    public enum TypeDescKind
    {
        /// <summary>
        /// The type is unknown (not a primitive, class, interface, struct, or type parameter)
        /// </summary>
        Unknown,

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
        /// A ref struct (stack-only type, cannot be boxed or stored in a field)
        /// </summary>
        RefStruct,
 
        /// <summary>
        /// Unconstrained type parameter
        /// </summary>
        UnconstrainedTypeParameter,

        /// <summary>
        /// Struct constrained type parameter
        /// </summary>
        StructTypeParameter,

        /// <summary>
        /// Class constrained type parameter
        /// </summary>
        ClassTypeParameter,
    }

    /// <summary>
    /// A decomposable member
    /// </summary>
    public class MemberDesc : IEquatable<MemberDesc>
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

        public bool Equals(MemberDesc other)
        {
            return this.Name == other.Name
                && this.Type.Equals(other.Type)
                && this.IsParameter == other.IsParameter;
        }

        public override bool Equals(object obj) =>
            obj is MemberDesc other && this.Equals(other);

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() 
                + this.Type.GetHashCode();
        }
    }
    #endregion

#if !T4
}
#endif
// #>