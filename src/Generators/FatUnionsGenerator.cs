// <#+
#if !T4
namespace UnionTypes.Toolkit.Generators
{
    using System.Linq;
#endif

#nullable enable

    public class FatUnionsGenerator : Generator
    {
        private readonly string _baseTypeName;
        private readonly int _maxTypeArgs;
        private readonly string _namespaceName;

        private FatUnionsGenerator(string baseTypeName, string namespaceName, int maxTypeArgs)
        {
            _baseTypeName = baseTypeName;
            _namespaceName = namespaceName;
            _maxTypeArgs = maxTypeArgs;
        }

        public static string Generate(string baseTypeName, string namespaceName, int maxTypeArgs)
        {
            var generator = new FatUnionsGenerator(baseTypeName, namespaceName, maxTypeArgs);
            generator.WriteFile();
            return generator.GeneratedText;
        }

        private void WriteFile()
        {
            WriteLine("using System;");
            WriteLine("using System.Collections.Generic;");
            WriteLine("using System.Diagnostics.CodeAnalysis;");
            WriteLine("#nullable enable");
            WriteLine();
            if (!string.IsNullOrEmpty(_namespaceName))
            {
                WriteLine($"namespace {_namespaceName}");
                WriteBraceNested(() =>
                {
                    WriteStandardUnionTypes();
                });
            }
            else
            {
                WriteStandardUnionTypes();
            }
        }

        private void WriteStandardUnionTypes()
        {
            for (int nTypeArgs = 2; nTypeArgs <= _maxTypeArgs; nTypeArgs++)
            {
                WriteStandardUnionType(nTypeArgs);
                if (nTypeArgs < _maxTypeArgs)
                    WriteLine();
            }
        }

        private int _nTypeArgs = 0;
        private string _typeArgList = "";
        private string _unionType = "";

        private void WriteStandardUnionType(int nTypeArgs)
        {
            _nTypeArgs = nTypeArgs;
            _typeArgList = string.Join(", ", Enumerable.Range(1, nTypeArgs).Select(n => $"T{n}"));
            _unionType = $"{_baseTypeName}<{_typeArgList}>";

            WriteLine($"[System.Runtime.CompilerServices.Union]");
            WriteLine($"public struct {_unionType}");
            WriteLineNested($": System.Runtime.CompilerServices.IUnion");
            WriteBraceNested(() =>
            {
                WriteLineSeparatedBlocks(() =>
                {
                    WriteBlock(() =>
                    {
                       WriteLine("private readonly int _kind;");
                       for (int i = 1; i <= _nTypeArgs; i++)
                       {
                           WriteLine($"private readonly T{i}? _value{i};");
                       }
                    });

                    WriteBlock(() =>
                    {
                        for (int i = 1; i <= _nTypeArgs; i++)
                        {
                            WriteLine($"public FatUnion(T{i} value) {{ _value{i} = value; _kind = value != null ? {i} : 0;}}");
                        }
                    });

                    WriteBlock(() =>
                    {
                        WriteLine("public bool HasValue => _kind != 0;");
                    });

                    for (int i = 1; i <= _nTypeArgs; i++)
                    {
                        WriteBlock(() =>
                        {
                            WriteLine($"public bool TryGetValue([NotNullWhen(true)] out T{i}? value)");
                            WriteBraceNested(() =>
                            {
                                WriteLine($"if (_kind == {i})");
                                WriteBraceNested(() =>
                                {
                                    WriteLine($"value = _value{i};");
                                    WriteLine("return value != null;");
                                });
                                WriteLine("else");
                                WriteBraceNested(() =>
                                {
                                    WriteLine("value = default;");
                                    WriteLine("return false;");
                                });
                            });
                        });
                    }

                    WriteBlock(() =>
                    {
                        WriteLine("public object? Value =>");
                        WriteNested(() =>
                        {
                            WriteLine("_kind switch");
                            WriteBraceNested(() =>
                            {
                                for (int i = 1; i <= _nTypeArgs; i++)
                                {
                                    WriteLine($"{i} => _value{i},");
                                }

                                WriteLine($"_ => null");
                            });
                        });
                        WriteLine(";");
                    });
                });
            });
        }
    }

#if !T4
}
#endif
// #>