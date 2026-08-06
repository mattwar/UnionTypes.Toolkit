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

    public class StandardUnionsGenerator : Generator
    {
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
                        WriteLine("public object? Value { get; private set;}");
                    });

                    WriteBlock(() =>
                    {
                        for (int i = 1; i <= _nTypeArgs; i++)
                        {
                            WriteLine($"public Union(T{i} value) {{ Value = value; }}");
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