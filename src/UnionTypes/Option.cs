namespace UnionTypes.Toolkit;

/// <summary>
/// A union that may contain either a <see cref="Some{T}"/> value or a <see cref="None"/> value.
/// It is similar to <see cref="System.Nullable{T}"/>, except the value can be also be a reference type and may use null as a valid value.
/// </summary>
[System.Runtime.CompilerServices.Union]
public struct Option<T>
    : System.Runtime.CompilerServices.IUnion
{   
    private readonly T _value;
    private readonly bool _hasValue;

    public Option(Some<T> value) 
    { 
        _value = value.Value;
        _hasValue = true;
    }

    public Option(None value)
    {
        // store None as null, so it matches the same state as when the struct is default-initialized.
        _value = default!;
        _hasValue = false;
    }

    public bool HasValue => true; // always has either some or none, so this is always true.

    public bool TryGetValue(out Some<T> value)
    {
        if (_hasValue)
        {
            value = new Some<T>(_value);
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public bool TryGetValue(out None value)
    {
        if (!_hasValue)
        {
            value = new None();
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public object Value => 
        _hasValue ? new Some<T>(_value) : new None();

    public static implicit operator Option<T>(T value) => new Option<T>(new Some<T>(value));
}

/// <summary>
/// Represents an optional value that has a value.
/// </summary>
public record struct Some<T>(T Value);

/// <summary>
/// Represents an optional value that has no value.
/// </summary>
public record struct None;


public static class Option
{
    public static None None => new None();

    public static Some<T> Some<T>(T value) => new Some<T>(value);
}