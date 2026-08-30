using System.Diagnostics.CodeAnalysis;
using ErrorType=System.Exception;

namespace UnionTypes.Toolkit;

/// <summary>
/// A union that may contain either a <see cref="Success{TValue}"/> value or a <see cref="Failure{TError}"/> value.
/// </summary>
[System.Runtime.CompilerServices.Union]
public struct Result<TValue, TError>
    : System.Runtime.CompilerServices.IUnion
{
    private readonly byte _kind;
    private readonly TValue _value;
    private readonly TError _error;

    public Result(Success<TValue> value)
    {
        _kind = 1;
        _value = value.Value;
        _error = default!;
    }

    public Result(Failure<TError> value)
    {
        _kind = 2;
        _value = default!;
        _error = value.Error;
    }

    public bool HasValue => _kind != 0; // has no value if uninitialized, otherwise has either success or failure.

    public bool TryGetValue(out Success<TValue> value)
    {
        if (_kind == 1)
        {
            value = new Success<TValue>(_value);
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
        if (_kind == 2)
        {
            value = new Failure<TError>(_error);
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }   

    public object Value => _kind switch
    {
        1 => new Success<TValue>(_value),
        2 => new Failure<TError>(_error),
        _ => throw new System.InvalidOperationException("Result is uninitialized and has no value.")
    };

    public static implicit operator Result<TValue, TError>(TValue value) => new Result<TValue, TError>(new Success<TValue>(value));
    public static implicit operator Result<TValue, TError>(TError error) => new Result<TValue, TError>(new Failure<TError>(error));
}

/// <summary>
/// Represents a successful result in a Result union.
/// </summary>
public record struct Success<T>(T Value);

/// <summary>
/// Represents a failed result in a Result union.
/// </summary>
public record struct Failure<T>(T Error);


/// <summary>
/// Helper class for creating Result union instances.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a <see cref="Success{TValue}"/> instance with the specified value.
    /// </summary>
    public static Success<TValue> Success<TValue>(TValue value) => new Success<TValue>(value);

    /// <summary>
    /// Creates a <see cref="Failure{TError}"/> instance with the specified error.
    /// </summary>
    public static Failure<TError> Failure<TError>(TError error) => new Failure<TError>(error);
}