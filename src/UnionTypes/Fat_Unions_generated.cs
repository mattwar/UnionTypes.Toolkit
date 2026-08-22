// // using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#nullable enable

namespace UnionTypes.Toolkit
{
    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                _ => null
            }
        ;
    }

    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2, T3>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly T3? _value3;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}
        public FatUnion(T3 value) { _value3 = value; _kind = value != null ? 3 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T3? value)
        {
            if (_kind == 3)
            {
                value = _value3;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                3 => _value3,
                _ => null
            }
        ;
    }

    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2, T3, T4>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly T3? _value3;
        private readonly T4? _value4;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}
        public FatUnion(T3 value) { _value3 = value; _kind = value != null ? 3 : 0;}
        public FatUnion(T4 value) { _value4 = value; _kind = value != null ? 4 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T3? value)
        {
            if (_kind == 3)
            {
                value = _value3;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T4? value)
        {
            if (_kind == 4)
            {
                value = _value4;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                3 => _value3,
                4 => _value4,
                _ => null
            }
        ;
    }

    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2, T3, T4, T5>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly T3? _value3;
        private readonly T4? _value4;
        private readonly T5? _value5;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}
        public FatUnion(T3 value) { _value3 = value; _kind = value != null ? 3 : 0;}
        public FatUnion(T4 value) { _value4 = value; _kind = value != null ? 4 : 0;}
        public FatUnion(T5 value) { _value5 = value; _kind = value != null ? 5 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T3? value)
        {
            if (_kind == 3)
            {
                value = _value3;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T4? value)
        {
            if (_kind == 4)
            {
                value = _value4;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T5? value)
        {
            if (_kind == 5)
            {
                value = _value5;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                3 => _value3,
                4 => _value4,
                5 => _value5,
                _ => null
            }
        ;
    }

    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2, T3, T4, T5, T6>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly T3? _value3;
        private readonly T4? _value4;
        private readonly T5? _value5;
        private readonly T6? _value6;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}
        public FatUnion(T3 value) { _value3 = value; _kind = value != null ? 3 : 0;}
        public FatUnion(T4 value) { _value4 = value; _kind = value != null ? 4 : 0;}
        public FatUnion(T5 value) { _value5 = value; _kind = value != null ? 5 : 0;}
        public FatUnion(T6 value) { _value6 = value; _kind = value != null ? 6 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T3? value)
        {
            if (_kind == 3)
            {
                value = _value3;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T4? value)
        {
            if (_kind == 4)
            {
                value = _value4;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T5? value)
        {
            if (_kind == 5)
            {
                value = _value5;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T6? value)
        {
            if (_kind == 6)
            {
                value = _value6;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                3 => _value3,
                4 => _value4,
                5 => _value5,
                6 => _value6,
                _ => null
            }
        ;
    }

    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2, T3, T4, T5, T6, T7>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly T3? _value3;
        private readonly T4? _value4;
        private readonly T5? _value5;
        private readonly T6? _value6;
        private readonly T7? _value7;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}
        public FatUnion(T3 value) { _value3 = value; _kind = value != null ? 3 : 0;}
        public FatUnion(T4 value) { _value4 = value; _kind = value != null ? 4 : 0;}
        public FatUnion(T5 value) { _value5 = value; _kind = value != null ? 5 : 0;}
        public FatUnion(T6 value) { _value6 = value; _kind = value != null ? 6 : 0;}
        public FatUnion(T7 value) { _value7 = value; _kind = value != null ? 7 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T3? value)
        {
            if (_kind == 3)
            {
                value = _value3;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T4? value)
        {
            if (_kind == 4)
            {
                value = _value4;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T5? value)
        {
            if (_kind == 5)
            {
                value = _value5;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T6? value)
        {
            if (_kind == 6)
            {
                value = _value6;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T7? value)
        {
            if (_kind == 7)
            {
                value = _value7;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                3 => _value3,
                4 => _value4,
                5 => _value5,
                6 => _value6,
                7 => _value7,
                _ => null
            }
        ;
    }

    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2, T3, T4, T5, T6, T7, T8>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly T3? _value3;
        private readonly T4? _value4;
        private readonly T5? _value5;
        private readonly T6? _value6;
        private readonly T7? _value7;
        private readonly T8? _value8;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}
        public FatUnion(T3 value) { _value3 = value; _kind = value != null ? 3 : 0;}
        public FatUnion(T4 value) { _value4 = value; _kind = value != null ? 4 : 0;}
        public FatUnion(T5 value) { _value5 = value; _kind = value != null ? 5 : 0;}
        public FatUnion(T6 value) { _value6 = value; _kind = value != null ? 6 : 0;}
        public FatUnion(T7 value) { _value7 = value; _kind = value != null ? 7 : 0;}
        public FatUnion(T8 value) { _value8 = value; _kind = value != null ? 8 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T3? value)
        {
            if (_kind == 3)
            {
                value = _value3;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T4? value)
        {
            if (_kind == 4)
            {
                value = _value4;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T5? value)
        {
            if (_kind == 5)
            {
                value = _value5;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T6? value)
        {
            if (_kind == 6)
            {
                value = _value6;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T7? value)
        {
            if (_kind == 7)
            {
                value = _value7;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T8? value)
        {
            if (_kind == 8)
            {
                value = _value8;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                3 => _value3,
                4 => _value4,
                5 => _value5,
                6 => _value6,
                7 => _value7,
                8 => _value8,
                _ => null
            }
        ;
    }

    [System.Runtime.CompilerServices.Union]
    public struct FatUnion<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    : System.Runtime.CompilerServices.IUnion
    {
        private readonly int _kind;
        private readonly T1? _value1;
        private readonly T2? _value2;
        private readonly T3? _value3;
        private readonly T4? _value4;
        private readonly T5? _value5;
        private readonly T6? _value6;
        private readonly T7? _value7;
        private readonly T8? _value8;
        private readonly T9? _value9;

        public FatUnion(T1 value) { _value1 = value; _kind = value != null ? 1 : 0;}
        public FatUnion(T2 value) { _value2 = value; _kind = value != null ? 2 : 0;}
        public FatUnion(T3 value) { _value3 = value; _kind = value != null ? 3 : 0;}
        public FatUnion(T4 value) { _value4 = value; _kind = value != null ? 4 : 0;}
        public FatUnion(T5 value) { _value5 = value; _kind = value != null ? 5 : 0;}
        public FatUnion(T6 value) { _value6 = value; _kind = value != null ? 6 : 0;}
        public FatUnion(T7 value) { _value7 = value; _kind = value != null ? 7 : 0;}
        public FatUnion(T8 value) { _value8 = value; _kind = value != null ? 8 : 0;}
        public FatUnion(T9 value) { _value9 = value; _kind = value != null ? 9 : 0;}

        public bool HasValue => _kind != 0;

        public bool TryGetValue([NotNullWhen(true)] out T1? value)
        {
            if (_kind == 1)
            {
                value = _value1;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T2? value)
        {
            if (_kind == 2)
            {
                value = _value2;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T3? value)
        {
            if (_kind == 3)
            {
                value = _value3;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T4? value)
        {
            if (_kind == 4)
            {
                value = _value4;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T5? value)
        {
            if (_kind == 5)
            {
                value = _value5;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T6? value)
        {
            if (_kind == 6)
            {
                value = _value6;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T7? value)
        {
            if (_kind == 7)
            {
                value = _value7;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T8? value)
        {
            if (_kind == 8)
            {
                value = _value8;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGetValue([NotNullWhen(true)] out T9? value)
        {
            if (_kind == 9)
            {
                value = _value9;
                return value != null;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public object? Value =>
            _kind switch
            {
                1 => _value1,
                2 => _value2,
                3 => _value3,
                4 => _value4,
                5 => _value5,
                6 => _value6,
                7 => _value7,
                8 => _value8,
                9 => _value9,
                _ => null
            }
        ;
    }
}


