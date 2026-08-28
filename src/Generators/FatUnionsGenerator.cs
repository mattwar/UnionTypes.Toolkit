// <#+
#if !T4
namespace UnionTypes.Toolkit.Generators
{
    using System.Linq;
#endif

#nullable enable

    public class FatUnionsGenerator
    {
        private readonly CodeWriter _writer = new CodeWriter();
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
            return generator._writer.WrittenText;
        }

        private void WriteFile()
        {
            _writer.WriteLine("using System;");
            _writer.WriteLine("using System.Collections.Generic;");
            _writer.WriteLine("using System.Diagnostics.CodeAnalysis;");
            _writer.WriteLine("#nullable enable");
            _writer.WriteLine();
            if (!string.IsNullOrEmpty(_namespaceName))
            {
                _writer.WriteLine($"namespace {_namespaceName}");
                _writer.WriteBraceNested(() =>
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
                    _writer.WriteLine();
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

            _writer.WriteLine($"[System.Runtime.CompilerServices.Union]");
            _writer.WriteLine($"public struct {_unionType}");
            _writer.WriteLine($": System.Runtime.CompilerServices.IUnion");
            _writer.WriteBraceNested(() =>
            {
                _writer.WriteLineSeparatedBlocks(() =>
                {
                    _writer.WriteBlock(() =>
                    {
                       _writer.WriteLine("private readonly int _kind;");
                       for (int i = 1; i <= _nTypeArgs; i++)
                       {
                           _writer.WriteLine($"private readonly T{i}? _value{i};");
                       }
                    });

                    _writer.WriteBlock(() =>
                    {
                        for (int i = 1; i <= _nTypeArgs; i++)
                        {
                            _writer.WriteLine($"public FatUnion(T{i} value) {{ _value{i} = value; _kind = value != null ? {i} : 0;}}");
                        }
                    });

                    _writer.WriteBlock(() =>
                    {
                        _writer.WriteLine("public bool HasValue => _kind != 0;");
                    });

                    for (int i = 1; i <= _nTypeArgs; i++)
                    {
                        _writer.WriteBlock(() =>
                        {
                            _writer.WriteLine($"public bool TryGetValue([NotNullWhen(true)] out T{i}? value)");
                            _writer.WriteBraceNested(() =>
                            {
                                _writer.WriteLine($"if (_kind == {i})");
                                _writer.WriteBraceNested(() =>
                                {
                                    _writer.WriteLine($"value = _value{i};");
                                    _writer.WriteLine("return value != null;");
                                });
                                _writer.WriteLine("else");
                                _writer.WriteBraceNested(() =>
                                {
                                    _writer.WriteLine("value = default;");
                                    _writer.WriteLine("return false;");
                                });
                            });
                        });
                    }

                    _writer.WriteBlock(() =>
                    {
                        _writer.WriteLine("public object? Value =>");
                        _writer.WriteNested(() =>
                        {
                            _writer.WriteLine("_kind switch");
                            _writer.WriteBraceNested(() =>
                            {
                                for (int i = 1; i <= _nTypeArgs; i++)
                                {
                                    _writer.WriteLine($"{i} => _value{i},");
                                }

                                _writer.WriteLine($"_ => null");
                            });
                        });
                        _writer.WriteLine(";");
                    });
                });
            });
        }
    }

#if !T4
}
#endif
// #>