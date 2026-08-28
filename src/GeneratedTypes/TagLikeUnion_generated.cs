// // using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#nullable enable
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8605
#pragma warning disable CS8618

[System.Runtime.CompilerServices.Union]
public partial struct TagLikeUnion : System.Runtime.CompilerServices.IUnion
{
    private readonly int _kind;
    private readonly Overlapped _overlapped;

    [StructLayout(LayoutKind.Explicit)]
    private struct Overlapped
    {
        [FieldOffset(0)] public float Case1;
        [FieldOffset(0)] public float Case2;
    }

    public record struct Open(float Percent);
    public record struct Closed(float Percent);

    public TagLikeUnion(Open value)
    {
        _kind = 1;
        value.Deconstruct(out var v);
        _overlapped.Case1 = v;
    }

    public TagLikeUnion(Closed value)
    {
        _kind = 2;
        value.Deconstruct(out var v);
        _overlapped.Case2 = v;
    }

    private Open GetCase1()
    {
        var tmp0 = _overlapped.Case1;
        return new (tmp0);
    }

    private Closed GetCase2()
    {
        var tmp0 = _overlapped.Case2;
        return new (tmp0);
    }

    public object? Value =>
        _kind switch
        {
            1 => this.GetCase1(),
            2 => this.GetCase2(),
            _ => null
        };

    public bool HasValue => _kind != 0;

    public bool TryGetValue([NotNullWhen(true)] out Open value)
    {
        if (_kind == 1)
        {
            value = this.GetCase1();
            return true;
        }
        else
        {
            value = default!;
            return false;
        }
    }

    public bool TryGetValue([NotNullWhen(true)] out Closed value)
    {
        if (_kind == 2)
        {
            value = this.GetCase2();
            return true;
        }
        else
        {
            value = default!;
            return false;
        }
    }
}

