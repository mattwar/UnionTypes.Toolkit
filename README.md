# UnionTypes.Toolkit

[![CI](https://github.com/mattwar/UnionTypes.Toolkit/actions/workflows/ci.yml/badge.svg)](https://github.com/mattwar/UnionTypes.Toolkit/actions/workflows/ci.yml)
[![GitHub release](https://img.shields.io/github/v/release/mattwar/UnionTypes.Toolkit)](https://github.com/mattwar/UnionTypes.Toolkit/releases/latest)

This repo is the source beind two packages available on Nuget.

- [UnionTypes.Toolkit](#uniontypestoolkit) - a library of common custom union types  
   [![NuGet](https://img.shields.io/nuget/v/UnionTypes.Toolkit.Generator)](https://www.nuget.org/packages/UnionTypes.Toolkit.Generator)

- [UnionTypes.Toolkit.Generator](#uniontypestoolkitgenerator) - a source generator for non-boxing custom unions  
   [![NuGet](https://img.shields.io/nuget/v/UnionTypes.Toolkit)](https://www.nuget.org/packages/UnionTypes.Toolkit)


### What is this?

This repo was originally created as a design playground when working on the C# Union Types feature. It was meant to contain various hand built union types with a variety of implementation strategies and a general configurable source generator that would produce custom unions using the techniques being discussed during design meetings.
This was long before any actual design was settled on and language feature worked started.

The original nuget release years ago contained a very different concept of unions to what has now become a feature for C# 15. 

Both the toolkit library and the source generator have been updated to match the new C# Union Types feature, and both are now available for general use.

The source generator has been retrofitted to produce only unions matching the specification for C# Union Types feature and using the Custom Unions API's. 
The generator produces unions that do not box struct values by default.

### How to Contribute

Please report bugs here under issues or submit PRs to fix them if you prefer.  
Use discussions instead of issues to share ideas or make requests.

----
<br/>

## UnionTypes.Toolkit

A collection of common union types compatible with the C# Union Types feature.

**Option**

The `Option<TValue>` union type allows you to represent either `Some<TValue>` or `None` without boxing.

**Result**

The `Result<TValue, TError>` union type allows you to represent either `Success<TValue>` or `Failure<TError>` without boxing.

**Union**

A family of generic union types `Union<T1, T2>`, `Union<T1, T2, T3>`, etc.

You can use these without declaring a unique named union type.
The held value will be boxed, however.

**FatUnion**

A family of generic union types `FatUnion<T1, T2>`, `FatUnion<T1, T2, T3>`, etc.  

You can use these without declaring a unique named union type.
The held value will not be boxed, but the type requires a memory footprint similar to using a tuple.

----
<br/>

## UnionTypes.Toolkit.Generator

This library implements a C# source generator for generating non-boxing custom union types compatible with the C# union types feature. It may contain additional generators in the future.

The generator is purely standalone; the generated union source does not depend on the union type library or any other external library beyond the standard dotnet runtime to function.

## Declaring a Non-Boxing Custom Union Type

Declare a partial record struct type with a partial void `Cases` method, whose parameters denote the case types for the union. 
The names of the parameters are not used, so any name will do.

```csharp
public partial record struct MyUnion
{
    partial void Cases(
        int case1, 
        float case2,
        string case3,        
        IManifest case4,
        Coordinate case5,
        Address case6
        );
}

record struct Coordinate(float Longitude, float Latitude);
record struct Address(int Id, string Name);
interface IManifest { ... }
```

The generator will layout the contents of the custom union so that the fields storing the different case types are overlapped with each other using the same memory space, if possible, or at least consume less space than simply having separate fields for each case type. By default, no case value is boxed.

In this example, there will be a single field storing a struct that contains enough space to store either an int, float, Coordinate or the address ID and a separate object field used to store either a string, IManifest or the address name.

## Further Customization

You can override the layout algorithm per case by using annotations in the comments preceding each case type parameter declaration.

**@box** - store a boxed value-type in an object field.  
**@isolate** - use a separate strongly typed field.  
**@overlap** - put the case's field in a shared/overlap memory space with other cases.  
**@decompose** - break down the value into its members and store the overlappable and non-overlappable part separately.

For example, if a struct cannot be overlapped with other cases because it contains one or more reference type members, the struct will be decomposed into its members if possible, and those members will be stored, and then the value will be recomposed from those parts when accessed.

If you don't want this to happen, you can use the @isolate annotation to keep the value whole by using an extra field to store it, increasing the memory footprint of the union, or use the @box annotation to box it instead and store it in the same field used to store the string or IManifest cases.

```csharp
public partial record struct MyUnion
{
    partial void Cases(
        int case1, 
        float case2,
        string case3,        
        IManifest case4,
        // @box 
        Coordinate case5,   // store this as a boxed object
        // @isolate
        Address case6       // use a separate field for this one
        );
}

record struct Coordinate(float Longitude, float Latitude);
record struct Address(int Id, string Name);
interface IManifest { ... }
```

### @overlap

The source generator assumes the metadata found in external libraries to be accurate and will automatically choose to overlap a struct type if all fields are overlappable types.

Typically, specifying the `@overlap` annotation does nothing since the case would have already been chosen to be overlapped. However, you may want to add the annotation for clarity in the source code.

However, if you specify `@overlap` on a type known to not be overlappable, the generator will produce a warning and not overlap the type.

```csharp
public partial record struct MyUnion
{
    partial void Cases(
        int case1, 
        // @overlap 
        string case2,        // warning! not overlappable
        // @overlap
        GoodStruct case3,    // overlappable, so this is a no-op
        // @overlap
        BadStruct case4      // warning! not overlappable
        );
}

public record struct GoodStruct(int x, float y);
public record struct BadStruct(int X, string Y);
```

### @decompose

Like with overlapping, the source generator assumes the metadata found in the external libraries to be accurate.

The generator will choose to decompose a struct if it is not overlappable and determines it can trust the shape of the type. Structs are trusted if its constructors have matching deconstructors and additional fields and properties are public and assignable in order to recompose the value when retrieved from the union. If it finds other non-public fields unrelated to assignable auto-properties it will not choose to decompose the type and instead store it as an isolated field.

If the generator does not choose to decompose your struct case, but you are confident it can be trusted you can specify the `@decompose` annotation to request it do so.

```csharp
public partial record struct MyUnion
{
    partial void Cases(
        GoodStructA structA,    // automatically decomposed
        GoodStructB structB,    // ...
        GoodStructC structC,    // ...
        GoodStructD structD,    // ...  
        // @decompose
        BadStructE structE,     // okay, if you say so, but Z will be lost
        // @decompose
        BadStructF structF      // Warning! will not decompose
        );
}

public record struct GoodStructA(int X, string Y);
public record struct GoodStructB(int X) { public string Y { get; init; } }
public record struct GoodStructC { public int X { get; init; } public string Y { get; init; } }
public struct GoodStructD { public int X; public string Y; }
public record struct BadStructE(int X) { public string Y; internal float Z; }
public struct BadStructF(int x) { private readonly int _x = x; public int X => _x; }
```

Some structs can still not be decomposed even if you request it. This may happen for reasons such as the generator's inability to determine how constructor parameters correspond to properties. If `BadStructF` had a matching deconstructor to its constructor or a default constructor and X was assignable, then it would have been allowed to decompose.

### @isolate

You can choose to have any case isolated into its own field. This happens automatically, if the case cannot be overlapped or decomposed.  If neither overlapping or decomposition is suitable, you can specify the `@isolate` annotation to force it to be isolated as whole.  

```csharp
public partial record struct MyUnion
{
    partial void Cases(
        int case1,
        float case2,
        // @isolate
        SketchyStruct case3    // good idea
        );
}
```

This can be a good idea if you have reasons to believe that metadata may not be accurate and the type may contain either non-overlappable members or contain data you don't wish to lose from decomposition.

> Note: If you attempt to isolate a reference type, it will still be stored using the boxed technique in an object field instead of a strongly-typed field. This is to improve field sharing across cases.

### @box

The source generator will never choose to box a struct value, but you can request it using the `@box` annotation.

```csharp
public partial record struct MyUnion
{
    partial void Cases(
        // @box
        int case1, 
        string case2
        );
}
```

*You can choose this an alternative to using @isolate when you believe the metadata for a case type is incomplete and should not be overlapped or decomposed.*


