// <#+
#if !T4
namespace UnionTypes.Toolkit.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
#endif

#nullable enable

    public class StandardUnionsGenerator
    {
        private readonly CodeWriter _writer = new CodeWriter();
        private readonly string _baseTypeName;
        private readonly int _maxTypeArgs;
        private readonly string _namespaceName;

        private StandardUnionsGenerator(string baseTypeName, string namespaceName, int maxTypeArgs)
        {
            _baseTypeName = baseTypeName;
            _maxTypeArgs = maxTypeArgs;
            _namespaceName = namespaceName;
        }

        public static string Generate(string baseTypeName, string namespaceName, int maxTypeArgs)
        {
            var generator = new StandardUnionsGenerator(baseTypeName, namespaceName, maxTypeArgs);
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
            _writer.WriteLineNested($": System.Runtime.CompilerServices.IUnion");
            _writer.WriteBraceNested(() =>
            {
                _writer.WriteLineSeparatedBlocks(() =>
                {
                    _writer.WriteBlock(() =>
                    {
                        _writer.WriteLine("public object? Value { get; private set;}");
                    });

                    _writer.WriteBlock(() =>
                    {
                        for (int i = 1; i <= _nTypeArgs; i++)
                        {
                            _writer.WriteLine($"public Union(T{i} value) {{ Value = value; }}");
                        }
                    });
                });
            });
        }
    }

#if !T4
}
#endif
// #>