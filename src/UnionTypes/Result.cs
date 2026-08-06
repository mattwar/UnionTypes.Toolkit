using System.Diagnostics.CodeAnalysis;
using ErrorType=System.Exception;

namespace UnionTypes.Toolkit;

[System.Runtime.CompilerServices.Union]
public struct Result<TValue, TError>
    : System.Runtime.CompilerServices.IUnion
{
    private readonly object? _value;

    private static readonly bool _typeArgsMatch = 
        typeof(TValue).IsAssignableTo(typeof(TError))
        || typeof(TError).IsAssignableFrom(typeof(TValue));

    public Result(Success<TValue> value)
    {
        if (_typeArgsMatch)
        {
            // both TValue and TError have intersecting types, so we must store the value as the Success<TValue> boxed.
            _value = value;
        }
        else if (value.Value == null)
        {
            // the success value is itself null, use the pre-boxed default.
            _value = _successDefaultBoxed;
        }
        else
        {
            // Store the non-null success value itself.
            // This does not incur boxing of the Success<TValue> struct, but it does incur boxing of the TValue value if it is a value type.
            _value = value.Value;
        }
    }

    public Result(Failure<TError> value)
    {
        if (_typeArgsMatch)
        {
            // both TValue and TError have intersecting types, so we must store the value as the Failure<TError> boxed.
            _value = value;
        }
        else if (value.Error == null)
        {
            _value = _failureDefaultBoxed;
        }
        else       
        {
            // Store the failure value itself.
            // This does not incur boxing of the Failure<TError> struct, but it does incur boxing of the TError value if it is a value type.
            _value = value.Error;
        }
    }

    public bool HasValue => _value != null;

    public bool TryGetValue(out Success<TValue> value)
    {
        if (_value is TValue val)
        {
            value = new Success<TValue>(val);
            return true;
        }
        else if (_value is Success<TValue> successValue)
        {
            value = successValue;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public bool TryGetValue(out Failure<TError> value)
    {
        if (_value is TError err)
        {
            value = new Failure<TError>(err);
            return true;
        }
        else if (_value is Failure<TError> failureValue)
        {
            value = failureValue;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }   

    public object? Value => _value switch
    {
        TValue val => new Success<TValue>(val),
        TError err => new Failure<TError>(err),
        Success<TValue> succ => succ,
        Failure<TError> fail => fail,
        _ => null
    };

    public static implicit operator Result<TValue, TError>(TValue value) => new Result<TValue, TError>(new Success<TValue>(value));
    public static implicit operator Result<TValue, TError>(TError error) => new Result<TValue, TError>(new Failure<TError>(error));

    private readonly object _successDefaultBoxed = new Success<TValue>(default!);
    private readonly object _failureDefaultBoxed = new Failure<TError>(default!);
}


public record struct Success<T>(T Value);
public record struct Failure<T>(T Error);


public static class Result
{
    public static Success<TValue> Success<TValue>(TValue value) => new Success<TValue>(value);
    public static Failure<TError> Failure<TError>(TError error) => new Failure<TError>(error);
}