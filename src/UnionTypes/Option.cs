namespace UnionTypes.Toolkit;

[System.Runtime.CompilerServices.Union]
public struct Option<T>
    : System.Runtime.CompilerServices.IUnion
{   
    private readonly object? _value;

    private static readonly bool _isNoneType = typeof(T) == typeof(None);

    public Option(Some<T> value) 
    { 
        if (_isNoneType)
        {
            // some cheeky user has used the None type as the value type.
            _value = _someOfNoneBoxed;
        }
        else
        {
            _value = value.Value;            
        }
    }

    public Option(None value)
    {
        // store None as null, so it matches the same state as when the struct is default-initialized.
        _value = null;
    }

    public bool HasValue => false; // we return None if null, so HasValue is always false

    public bool TryGetValue(out Some<T> value)
    {
        if (_value is T val)
        {
            value = new Some<T>(val);
            return true;
        }
        else if (_value is Some<T> someValue)
        {
            value = someValue;
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
        if (_value == null)
        {
            value = Option.None;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public object Value => _value switch
    {
        null => _noneBoxed,
        T val => val,
        _ => _value
    };

    public static implicit operator Option<T>(T value) => new Option<T>(new Some<T>(value));   

    private readonly object _noneBoxed = new None();
    private readonly object _someOfNoneBoxed = new Some<None>(new None());
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