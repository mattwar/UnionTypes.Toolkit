﻿// 
// 
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnionTypes;
#nullable enable

namespace UnionTypes
{
    public partial struct Result<TValue, TError> : IClosedTypeUnion<Result<TValue, TError>>, IEquatable<Result<TValue, TError>>
    {
        public enum Case
        {
            Success = 1,
            Failure = 2,
        }

        public Case Kind { get; }
        private readonly TValue _field0;
        private readonly TError _field1;

        private Result(Case kind, TValue field0, TError field1)
        {
            this.Kind = kind;
            _field0 = field0;
            _field1 = field1;
        }

        public static Result<TValue, TError> Success(TValue value) => new Result<TValue, TError>(kind: Result<TValue, TError>.Case.Success, field0: value, field1: default!);
        public static Result<TValue, TError> Failure(TError error) => new Result<TValue, TError>(kind: Result<TValue, TError>.Case.Failure, field0: default!, field1: error);

        public static implicit operator Result<TValue, TError>(TValue value) => Result<TValue, TError>.Success(value);
        public static implicit operator Result<TValue, TError>(TError value) => Result<TValue, TError>.Failure(value);

        /// <summary>Accessible when <see cref="Kind"> is <see cref="Case.Success">.</summary>
        public TValue Value => this.Kind == Result<TValue, TError>.Case.Success ? _field0 : default!;
        /// <summary>Accessible when <see cref="Kind"> is <see cref="Case.Failure">.</summary>
        public TError Error => this.Kind == Result<TValue, TError>.Case.Failure ? _field1 : default!;

        #region ITypeUnion, ITypeUnion<TUnion>, ICloseTypeUnion, ICloseTypeUnion<TUnion> implementation.
        public static bool TryCreate<TCreate>(TCreate value, out Result<TValue, TError> union)
        {
            switch (value)
            {
                case TValue v: union = Success(v); return true;
                case TError v: union = Failure(v); return true;
            }

            if (value is ITypeUnion u && u.TryGet<object>(out var uvalue))
            {
                return TryCreate(uvalue, out union);
            }

            var index = TypeUnion.GetTypeIndex<Result<TValue, TError>, TCreate>(value);
            switch (index)
            {
                case 0 when TypeUnion.TryCreate<TCreate, TValue>(value, out var v0): union = Success(v0); return true;
                case 1 when TypeUnion.TryCreate<TCreate, TError>(value, out var v1): union = Failure(v1); return true;
            }

            union = default!; return false;
        }

        private static IReadOnlyList<Type> _types = new [] {typeof(TValue), typeof(TError)};
        static IReadOnlyList<Type> IClosedTypeUnion<Result<TValue, TError>>.Types => _types;
        private int GetTypeIndex()
        {
            switch (Kind)
            {
                case Result<TValue, TError>.Case.Success: return 0;
                case Result<TValue, TError>.Case.Failure: return 1;
                default: return -1;
            }
        }
        Type ITypeUnion.Type { get { var index = this.GetTypeIndex(); return index >= 0 && index < _types.Count ? _types[index] : typeof(object); } }

        public bool TryGet<TGet>([NotNullWhen(true)] out TGet value)
        {
            switch (this.Kind)
            {
                case Result<TValue, TError>.Case.Success:
                    if (this.Value is TGet tvSuccess)
                    {
                        value = tvSuccess;
                        return true;
                    }
                    return TypeUnion.TryCreate(this.Value, out value);
                case Result<TValue, TError>.Case.Failure:
                    if (this.Error is TGet tvFailure)
                    {
                        value = tvFailure;
                        return true;
                    }
                    return TypeUnion.TryCreate(this.Error, out value);
            }
            value = default!; return false;
        }
        #endregion

        public bool Equals(Result<TValue, TError> other)
        {
            if (this.Kind != other.Kind) return false;

            switch (this.Kind)
            {
                case Result<TValue, TError>.Case.Success:
                    return object.Equals(this.Value, other.Value);
                case Result<TValue, TError>.Case.Failure:
                    return object.Equals(this.Error, other.Error);
                default:
                    return false;
            }
        }

        public override bool Equals(object? other)
        {
            return TryCreate(other, out var union) && this.Equals(union);
        }

        public override int GetHashCode()
        {
            switch (this.Kind)
            {
                case Result<TValue, TError>.Case.Success:
                    return this.Value?.GetHashCode() ?? 0;
                case Result<TValue, TError>.Case.Failure:
                    return this.Error?.GetHashCode() ?? 0;
                default:
                    return 0;
            }
        }

        public static bool operator == (Result<TValue, TError> left, Result<TValue, TError> right) => left.Equals(right);
        public static bool operator != (Result<TValue, TError> left, Result<TValue, TError> right) => !left.Equals(right);

        public override string ToString()
        {
            switch (this.Kind)
            {
                case Result<TValue, TError>.Case.Success:
                    return this.Value?.ToString() ?? "";
                case Result<TValue, TError>.Case.Failure:
                    return this.Error?.ToString() ?? "";
                default:
                    return "";
            }
        }

        public void Match(Action<TValue> whenSuccess, Action<TError> whenFailure, Action? whenInvalid = null)
        {
            switch (Kind)
            {
                case Result<TValue, TError>.Case.Success : whenSuccess(this.Value); break;
                case Result<TValue, TError>.Case.Failure : whenFailure(this.Error); break;
                default: if (whenInvalid != null) whenInvalid(); else throw new InvalidOperationException("Unhandled invalid union state."); break;
            }
        }

        public TResult Select<TResult>(Func<TValue, TResult> whenSuccess, Func<TError, TResult> whenFailure, Func<TResult>? invalid = null)
        {
            switch (Kind)
            {
                case Result<TValue, TError>.Case.Success: return whenSuccess(this.Value);
                case Result<TValue, TError>.Case.Failure: return whenFailure(this.Error);
                default: return invalid != null ? invalid() : throw new InvalidOperationException("Unhandled invalid union state.");
            }
        }
    }
}
