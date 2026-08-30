# UnionTypes.Toolkit

A collection of common union types compatible with the C# Union Types feature.

**Option**

The `Option<TValue>` union type allows you to represent either `Some<TValue>` or `None`. 

**Result**

The `Result<TValue, TError>` union type allows you to represent either `Success<TValue>` or `Failure<TError>`.

**Union**

A family of generic union types `Union<T1, T2>`, `Union<T1, T2, T3>`, etc.
You can use these without declaring a unique named union type.
The held value will be boxed, however.

**FatUnion**

A family of generic union types `FatUnion<T1, T2>`, `FatUnion<T1, T2, T3>`, etc.  
You can use these without declaring a unique named union type.
The held value will not be boxed, but the type has space for all cases, similar to a tuple.

[Learn about using the toolkit in your project here.](https://github.com/mattwar/UnionTypes.Toolkit)
