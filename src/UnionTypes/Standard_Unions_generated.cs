// // using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#nullable enable

namespace UnionTypes.Toolkit
{
    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
    }

    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2, T3>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
        public Union(T3 value) { Value = value; }
    }

    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2, T3, T4>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
        public Union(T3 value) { Value = value; }
        public Union(T4 value) { Value = value; }
    }

    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2, T3, T4, T5>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
        public Union(T3 value) { Value = value; }
        public Union(T4 value) { Value = value; }
        public Union(T5 value) { Value = value; }
    }

    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2, T3, T4, T5, T6>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
        public Union(T3 value) { Value = value; }
        public Union(T4 value) { Value = value; }
        public Union(T5 value) { Value = value; }
        public Union(T6 value) { Value = value; }
    }

    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2, T3, T4, T5, T6, T7>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
        public Union(T3 value) { Value = value; }
        public Union(T4 value) { Value = value; }
        public Union(T5 value) { Value = value; }
        public Union(T6 value) { Value = value; }
        public Union(T7 value) { Value = value; }
    }

    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2, T3, T4, T5, T6, T7, T8>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
        public Union(T3 value) { Value = value; }
        public Union(T4 value) { Value = value; }
        public Union(T5 value) { Value = value; }
        public Union(T6 value) { Value = value; }
        public Union(T7 value) { Value = value; }
        public Union(T8 value) { Value = value; }
    }

    [System.Runtime.CompilerServices.Union]
    public struct Union<T1, T2, T3, T4, T5, T6, T7, T8, T9>
        : System.Runtime.CompilerServices.IUnion
    {
        public object? Value { get; private set;}

        public Union(T1 value) { Value = value; }
        public Union(T2 value) { Value = value; }
        public Union(T3 value) { Value = value; }
        public Union(T4 value) { Value = value; }
        public Union(T5 value) { Value = value; }
        public Union(T6 value) { Value = value; }
        public Union(T7 value) { Value = value; }
        public Union(T8 value) { Value = value; }
        public Union(T9 value) { Value = value; }
    }
}


