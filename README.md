# UnionTypes.Toolkit

A library of common union types for dotnet and a C# source generator for creating custom ones.

*disclaimer: This repo started as an exploration of union types for introduction into C# and the dotnet runtime 
as part of the C# LDM (Language Design Meeting).
It is now published (by [me](https://github.com/mattwar)) for anyone to use, but probably mainly me.
It is not a product of Microsoft, and is not an indicator of what will or will not become a product.*

---
Table of Contents

- [Overview](#Overview)
- [Download the Toolkit and Generator](#Download-the-Toolkit-and-Generator)
- [Using the Included Union Types](#Using-the-Included-Union-Types)
- [Generating Custom Union Types](#Custom-Union-Types)

## Overview

The term *Union Type* is used here to refer generally to many kinds of unions, 
sometimes called discriminated unions, sum types, tagged unions, etc.
You may already be familiar with union types if you have used discriminated unions in F#, 
union types in Typescript or even a union structure in C++.

In short, a union type is any type that can exist in one of many explicit states or cases.
Each case may allow the type to hold onto different kinds of data.
For example, a C# enum is a union type, since each enum value is a unique case of the enum type,
but it is not a very interesting one because it does not carry any extra data with it.
A class heirarchy is also a union type, since each derived class is a unique case of the base type
and each can hold different kinds of data. If a class hierarchy is suitable for your needs, look no further.

This toolkit deals with special kinds of union types that solve problems that are not satisfactorily solved by class hierarchies alone.
Primarily these are cases where you are concerned about allocations or footprint (size of data).
It focuses on two categories of these union types, but there may be others.

- A *Type Union* is any type that can hold or represent a value of one of a set of unique types.
For instance, if you wanted to have a variable assignable to only either a Cat, Dog or Bird, 
you could use a type union to constrain the value to only one of those types.
If you already have a class hierarchy, for example Pet, of which all those types are derived from, then you don't need anything else. 
Just use the base type. However, if its not pratical to have a class hierachy of just the types you want to include, then a type union is a good alternative.

- A *Tag Union* is not limited to a set of unique types, but is instead constrained to a set of uniquely named cases (tags)
like an enum is, but each case may also carry with it its own unique set of variables. 
Tag unions (or traditional discriminated unions) originate from languages with a history of not having classes or inheritance.
Typical usage patterns in those languages include constructing the union from cases and values and later matching on the case and deconstructing the values back into local variables.

The types provided in the library are all presented as type unions to give you the option to interact with the case values outside of the union.
They each implement the `ITypeUnion` interface that enables conditionally constructing them, accessing their values, and converting them 
to other type unions. Tag unions share no commonality that would make this possible.

These types are provided because they are the ones the community typically asked for when discussing discriminated unions 
with the C# Language Design Team, and so got modelled for discussion.

- The `OneOf` type is a family of generic struct types that can hold a single value constrained to the set of types declared in its type arguments.
You may already have access to a type like this from another source or by a different name.
This is a good choice for most use cases, when the types you want to include are not already together in a hierarchy and are primarily already reference types,
but has the drawback of boxing value types, which could matter if your application is sensitive to GC pressure.

- The `Option` type is a type that is often built into languages to represent a value that may or may not be present,
similar to how some might use a null to represent the absence of a value. 
The benefit of the Option type is that you won't accidentally deference the null, causing an exception.
Languages with an Option (or Maybe) type typically have monadic operations that ferry the absence of a value back through your code,
automatically skipping that parts that would depend on the value without requiring you to constantly check.
This is similar to how the null conditional operator works in C#, but at a grander scale.
You won't be able to use it that way in C# but you can simulate a bit of it via some of the provided methods.

- The `Result` type is a type that is often built into languages to represent the result of an operation that may fail.
It represents a value that you return from a function that is either in the success state with its expected value or a failure state with an error.
Typically, languages that have this type also have monadic operations that ferry the failure through your code
without requiring you to explicitly unpack them to use the success value, similar to how you experience exceptions working in C#.
Its not possible to use it that way in C# but you can simulate a bit of it via some of the provided methods.

- The `Variant` type is a type union that is not actually constrained.
It can hold a value of any type, but will not box most primitives and small structs.
It does this by partially being a type union with a fixed number of known cases, 
and a catch-all case that also tries to avoid boxing at runtime for types that can fit in the provided space.
It is a good choice when you would have otherwise chosen to use `object`, 
but want to avoid boxing in common scenarios.

If none of these types seem suitable for your needs,
or you'd rather have your own type with its own name than repeatedly typing out all the case types as generic arguments
every time you refer to it, you can create a custom union type.
To do this, you can either write a completely custom type following your own rules, 
write a custom type following the same patterns and interfaces provided in this library to allow interop with features provided there,
or use the source generator to create the type for you from a partial declaration.

---

## Download the Toolkit and Generator

The toolkit is available as a nuget package on nuget.org.  
[Download Toolkit Here](https://www.nuget.org/packages/UnionTypes.Toolkit)

A separate source generator for generating custom union types is also available as a nuget package.  
[Download Generator Here](https://www.nuget.org/packages/UnionTypes.Toolkit.Generator)

---

## Using the Included Union Types

The toolkit library includes many predefined union types ready to use.

- [OneOf](#Using-the-OneOf-Types)
- [Option](#Using-the-Option-Type)
- [Result](#Using-the-Result-Type)
- [Variant](#Using-the-Variant-Type)

Additional helper classes and interfaces:

- ITypeUnion
- TypeUnion

---
## Using the OneOf Types

The `OneOf` type is a series of overloaded generic types.
Each `OneOf` type is a closed type union, only allowing values of types specified in the type arguments.
Use them to declare type unions of any class, struct, interface or array types.

- Not supported: refs, ref structs and pointers. That means no `Span<T>.` If you require types like `Span<T>` use a custom union. The generator supports generating ref struct unions.

- When instances of value types (structs in C#) are placed in the union, they are stored by boxing the value, which may cause GC pressure.

### Creating an Instance

You can create an instance of a `OneOf` type by calling one of the `Create` factory methods or via assignment.

```CSharp
    var number = OneOf<int, double>.Create(5);  //  using factory
    OneOf<int, double> number = 5;              //  using assignment.
```

You can also attempt to create an instance using the `TryCreate<T>` factory method that attempts to create an instance of the OneOf type
from an arbitrary value.

```CSharp
    object someValue = 5;
    if (OneOf<int, double>.TryCreate(someValue, out var number)) { ... })
```

You can also use `TryCreate` to attempt to create a OneOf from a different type union.

```CSharp
    OneOf<int, string> intOrString = 5;
    if (OneOf<int, double>.TryCreate(intOrString, out var number)) { ... })
```

### Accessing the Value

There are multiple ways to access the underlying value.

You can access it in a weakly-typed way by using the `Value` property.
```CSharp
   object value = number.Value;
```

You can access it in a strongly-typed way via explicit coersion to one of the known types.
However, this may throw an `InvalidCastException` if you use the wrong type.
```CSharp
    int x = (int)number;
```

You can access each possible strongly-typed value via additional value properties.
Every `OneOf` type has a value property for each type argument; `Type1Value`, `Type2Value`, etc.
You can determine which one of these is currently valid by checking the `Kind` property.
Accessing an invalid value property will return default.
```CSharp
    var isLessThan5 = number.Kind switch { 
        1 => number.Type1Value < 5,
        2 => number.Type2Value < 5.0
        _ => false
        };
```

You can also use the `Select` and `Match` methods to check the case and access the value automatically.

```CSharp
    var isLessThan5 = number.Select(
        i => i < 5,
        d => d < 5.0
    );
```

```CSharp
    number.Match(
        i => Console.WriteLine($"less than 5: {i < 5}"),
        d => Console.WriteLine($"less than 5.0: {d < 5.0}")
    );
```

Lastly, you can access arbitrarilly typed values via the `TryGet<T>` method.
```CSharp
    if (number.TryGet<int>(out var intValue)) { ... }
```
This technique is useful when you don't now the exact union type, and have a reference to `ITypeUnion`,
or when you want to access the value using a base class or interface, or via a different union type.

```CSharp
if (intOrDouble.TryGet<OneOf<int, string>>(out var intOrString)) { ... })
```

### Comparing Equality

Each `OneOf` type declares pass-through equality operators and implements `IEquatable<T>` so you can compare two instances of the same OneOf type.
If the values are the same, the OneOf instances will be considered the same.

``` CSharp
OneOf<int, double> number1 = 5;
OneOf<int, double> number2 = "five";
if (number1 == number2) { ... }
if (number1.Equals(number2)) { ... }
```
Since there are also implicit coercion operators, you can compare a OneOf instance with a value of one of the case types.

```CSharp
OneOf<int, double> number = 5;
if (number == 5) { ... }
if (number.Equals(5)) { ... }
```

You can also compare an instance of one `OneOf` type with an instance of another `OneOf` type, or any other type union that implements `ITypeUnion` 
using the generic `Equals` method.

```CSharp
OneOf<int, double> number = 5;
OneOf<int, string> value = 5;
if (number.Equals(value)) { ... }
```

#### Compare equality between different unions

``` CSharp
OneOf<int, string> value1 = 5;
OneOf<string, int> value2 = 5;
var areEqual = value1.Equals(value2);
```

---
## Using the Option Type

The `Option<T>` type is a type that can hold a value of type `T` or no value at all.

You create an instance of `Option<T>` by calling the factory method `Some` or by just assigning a value to a variable with that type.

```CSharp
    var number = Option<int>.Some(5);  //  using factory
    var number = Option.Some(5);       //  using factory and inferring the value type.
    Option<int> number = 5;            //  using assignment.
```

You obtain a `Option<T>` instance without a value by calling the the `None` factory, 
assigning a variable the `None.Singleton` instance or assigning a variable the default state.

```CSharp
    var noNumber = Option<int>.None();      // using factory
    Option<int> noNumber = None.Singleton;  // using singleton
    Option<int> noNumber = default;         // using default
```

You access the state of the option by checking the `Kind` property and the value using the `Value` property.
```CSharp
    switch (number.Kind)
    {
        case Option<int>.Case.Some:
            Console.WriteLine($"Its a value: {number.Value}");
            break;
        case Option<int>.Case.None:
            Console.WriteLine("Its nothing, really.");
            break;
    }
```

You can also use the `Match` and `Select` methods to handle each case.

```CSharp
    number.Match(
        value => Console.WriteLine($"Its a value: {value}"),
        () => Console.WriteLine("Its nothing, really.")
    );
```

```CSharp
    // convert non-values into values
    var newValue = number.Select(value => value * 2, () => 0);
```

There is also a `Map` method that allows you to transform the value, only if it currently exists.
```CSharp
    var mapped = number.Map(value => value * 2);
```

---
## Using the Result Type

The `Result<TValue,TError>` is a union that either holds a success value of type `TValue` or a failure value of type `TError`.

You can construct one using the factory methods `Success` and `Failure`, or by assigning a value or error to a variable of that type.

```CSharp
    var result = Result<int, Error>.Success(5);  //  using factory
    Result<int, Error> result = 5;               //  using assignment.
```
```CSharp
    var error = Result<int, string>.Failure(new Error("Whoops"));  //  using factory
    Result<int, string> error = new Error("Whoops");               //  using assignment.
```

*Note: If both the value and the error type are the same, then assignment will be ambiguous and you wil need to use the factory methods.*

Like with all the other unions, you can check the case via the `Kind` property and access the succesor or failure values
using the `Value` and `Error` properties respectively.

```CSharp
    switch (result.Kind)
    {
        case Result<int, Error>.Case.Success:
            Console.WriteLine($"Success: {result.Value}");
            break;
        case Result<int, Error>.Case.Failure:
            Console.WriteLine($"Failure: {result.Error.Message}");
            break;
    }
```

Alternatively, you can use the `Match` and `Select` methods.

```CSharp
    result.Match(
        value => Console.WriteLine($"Success: {value}"),
        error => Console.WriteLine($"Failure: {error.Message}")
    );
```
```CSharp
    var mapped = result.Select(value => $"{value * 2}", error => -1);
```

In addition, there is a `Map` method that allow you to transform the success value 
only when there is a success value and otherwise maintain the error state.

```CSharp
    var mapped = result.Map(value => $"{value * 2}");
```

---
## Using the Variant Type

The `Variant` type is a type union that is not closed to a fixed number of case types.
Any value of any type can be assigned or converted into a `Variant`, even the value `null`.
What makes a variant interesting is that it is designed to avoid boxing of many value and struct types.
If the value or struct type constains no reference type members and fits withing 64 bits, it will be stored within the variant without boxing.

### Creating an Instance

You can create an instance of a `Variant` by calling one of the `Create` factory methods or 
via assignment and implicit coercion if the value is one of the known primitives.

```CSharp
    var number = Variant.Create(5);  //  using factory
    Variant number = 5;              //  using assignment.
    Variant point = Variant.Create(new Point(3, 4)); // factory only if its not a known primitive.
```

You can also create a variant using the `TryCreate<T>` method. However, it will always succeed, since variants may contain any value.
It exists on the type to satisfy the contract of the `ITypeUnion<T>` interface.

### Accessing the Value

You can access the value in a weakly-typed way by using the `Value` property.
However, this may cause boxing if the value is a struct type.

```CSharp
   object value = variant.Value;
```

You can access the strongly-typed value via any of the specialized value properties.
The `Variant` type has a specialized value properties for many well known types; `Int32Value`, `DoubleValue`, `StringValue` etc.
You can determine which of these is the correct property to access using the `Kind` property.

```CSharp
    var isLessThan5 = variant.Kind switch { 
        VariantKind.Int32 => variant.Int32Value < 5,
        VariantKind.Double => variant.DoubleValue < 5.0,
        VariantKind.Int64 => variant.Int64Value < 5L,
        _ => false
        };
```

You can attempt to access arbitrary strongly-typed values using the `TryGet<T>` method.

```CSharp
    if (variant.TryGet<Point>(out var point)) { ... }
```
There is also `Get<T>` that returns the value if possible or throws an `InvalidCastException`,
`GetOrDefault<T>` that returns the value if possible or `default`,
and `CanGet<T>` that tells you if the `TryGet<T>` method will succeed.

If you want to know the value's type without potential boxing, you can use the `Type` property.

```CSharp
    var currentType = variant.Type;
```

### Nulls

The `Variant` can store a null value.

You can determine if the current value is `null`, by checking the `IsNull` property or by comparing the `Kind` property to `VariantKind.Null`.
```CSharp
    if (variant.IsNull) { ... }
    if (variant.Kind == VariantKind.Null) { ... }
```

*The `TryGet<T>` and `CanGet<T>` methods will return false when the value is null, even if the type `T` can contain a null value; for example, `int?` or `string?`.*


---
## Custom Union Types

Custom union types can be generated for you using a C# source generator, given a partial type declartion.

The source generator creates union types with efficient storage layouts.
It does this by analyzing the data types involved and generating a layout that avoids boxing and minimizes the space needed to store the data from all the cases.
It cannot be as optimal as the layout of C++ unions, since the runtime does not allow overlapping the same memory with 
reference and value types, but it does what it can to overlap and reuse fields.


- [Choosing Between Type Unions and Tag Unions](#Choosing-Between-Type-Unions-and-Tag-Unions)  
- [Declaring a Type Union](#Declaring-a-Type-Union)  
- [Declaring a Tag Union](#Declaring-a-Tag-Union)  
- [Customizing the Generation with Attributes](#Customizing-the-Generation-with-Attributes)  
- [Declaring a Case Factory as a Property](#Declaring-a-Case-Factory-as-a-Property)  
- [Declaring a Case without an Accessor](#Declaring-a-Case-without-an-Accessor)  
- [Assigning Specific Tag Values to Cases](#Assigning-Specific-Tag-Values-to-Cases)  
- [Generate Union Types without the Toolkit](#Generating-Union-Types-without-the-Toolkit)
- [Unions with Spans](#Unions-with-Spans)

### Choosing Between Type Unions and Tag Unions

Both generated type unions and tag unions are very similar.
Given the same cases and the same fundemental data, the two types will end up structurally identical internally.
The difference lies in the methods and operators and interfaces provided.

Since a type union is constrained to a set of unique types, it can provide more features than a tag union.
The types included in the union are its cases, meaning multiple type unions can have some of the exact same cases
and it is possible to have common generic interfaces to interact with the contents of any type union.

This is not possible with a tag union. Since multiple cases, even with ones that only contain a single value, may contain values of the same type,
knowing the value's type is not enough information to determine which case the value represents.
Likewise, even having cases with the same tag names and the same kinds of values on two different unions is not enough
to know that the two union's cases are meant to represent the same thing. It may just be coincidence.

Specifically, a type union has these additional features:
- Implicit coercion operators you can use to assign a value directly to the union without explicitly calling a factory method.
- An implementation of the `ITypeUnion` interface enabling abstraction over your union and coercion between unions with similar cases.

So, in short, if you want to know how to choose between them:

- Use a type union when you want to constrain a variable to a set of types that are not already a class hierarchy containing just those types,
and those types are useful in your application beyond just being cases of the union.

- Use a tag union when the cases don't make sense in your application outside of the union,
  or you prefer the simplicity of the union without the additional operators, methods and interfaces.


### Declaring a Type Union

You can declare a type union by declaring a partial struct with a `TypeUnion` attribute,
and a partial factory method for each case type. 

*Be careful to not declare the type nested inside another type, or as part of a top-level statements file.*

```CSharp
    [TypeUnion]
    public partial struct Pet
    {
        public static partial Pet Create(Cat cat);
        public static partial Pet Create(Dog dog);
        public static partial Pet Create(Bird bird);      
    }
```

As soon as its declared (and the source generator has successfully run) you can start using the type union.

Assign a value directly to a variable.
```CSharp
    Pet pet = new Cat("Mr Fluffy");
```
Alternatively, use the factory method to create the union.
```CSharp
    Cat cat = new Cat("Mr Fluffy");
    Pet pet = Pet.Create(cat);
```
Switch on the case and access the value via strongly typed properties.
```CSharp
    switch (pet.Kind)
    {
        case Pet.Case.Cat: 
            Console.WriteLine($"Cat's name is {pet.CatValue.Name}");
            break;
        case Pet.Case.Dog:
            Console.WriteLine($"Dog's name is {pet.DogValue.Name}");
            break;
        case Pet.Case.Bird:
            Console.WriteLine($"Bird's name is {pet.BirdValue.Name}");
            break;
    }
```
By default the type will have a property named `Kind` that returns an enum named `Case` with names for each case taken from the case type itself.
The values for each case can be accessed via strongly typed properties called `[case]Value`.

Alternatively, you can use the `Match` and `Select` methods instead of a switch statement
and skip referring to the kind and value properties, but potentially cause delegate allocations due to capture.

```CSharp
    pet.Match(
        cat => Console.WriteLine($"Cat's name is {cat.Name}"),
        dog => Console.WriteLine($"Dog's name is {dog.Name}"),
        bird => Console.WriteLine($"Bird's name is {bird.Name}")
    );
```

```CSharp
    var saying = pet.Select(
        cat => "meow",
        dog => "woof",
        bird => "chirp"
    );
```

You can also compare two different instances of the same type union for equality without needing to access the values.

```CSharp
    Pet pet1 = new Cat("Mr Fluffy");
    Pet pet2 = new Dog("Spot");
    if (pet1 == pet2) { ... }
```

### Declaring a Tag Union

You declare a tag union by declaring a partial struct with a `TagUnion` attribute,
and a partial factory method for each case.

```CSharp
    [TagUnion]
    public partial struct Pet
    {
        public static partial Pet Cat(string name, int toys);
        public static partial Pet Dog(string name, bool friendly);
        public static partial Pet Bird(string name, string[] thingsItSays);
    }
```

You use it similarly to how type unions are used, without some of the additional API like coercion operators,
generic factories and accessors.
.

```CSharp
    Pet pet = Pet.Cat("Mr Fluffy");
    ...
    switch (pet.Kind)
    {
        case Pet.Case.Cat: 
            Console.WriteLine($"Cat's name is {pet.CatValues.name}");
            break;
        case Pet.Case.Dog:
            Console.WriteLine($"Dog's name is {pet.DogValues.name}");
            break;
        case Pet.Case.Bird:
            Console.WriteLine($"Bird's name is {pet.BirdValues.name}");
            break;
    }
```
In addition, some of the generated accessor properties have different names than for type unions.

- If the case has multiple values, the property is named [case]Values and a tuple of all the values are returned.
- If the case has only one value, then the property is named [case]Value and just that one value is returned.
- If the case has no values, an Is[case] method is generated instead and returns `bool`.

*It is possible to customize all the factory and property names.*

A tag union also has `Match` and `Select` methods, just like the type union.

### Customizing the Generation with Attributes

You can customize the generated union by setting properties in the `TypeUnion` or `TagUnion` attribute.

| Property   | Type | Description   | Default |
|:-----------|:-----|:--------------|---------|
| DecomposeStructs | bool | Enables deconstruction of tuple and record structs to improve field resuse across cases. | true |
| DecomposeForeignStructs | bool | Enables deconstruction of tuple and record structs defined outside the compilation unit. Metadata for foreign types may be incomplete. Disable this to allow correct storing of custom record structs that have additional data fields not accessible via deconstruction.| true |
| GenerateEquality | bool | Enables generation of `IEquatable<T>` implementation. | true |
| GenerateMatch | bool | Enables generation of `Select` and `Match` methods. | true |
| GenerateToString | bool | Enables generation of `ToString` override. | true |
| OverlapStructs | bool | Enables overlapping of values that can share the same memory across cases. This incluse primitive structs and structs containing only other overlappable fields. | true |
| OverlapForeignStructs | bool | Enables overlapping of structs defined outside the compilation unit. This is disabled by default because the metadata for foreign structs may be incomplete. Enable this only if you trust the types involved to truly be overlappable. | false |
| ShareSameTypeFields | bool | Enables reuse of fields with the same type across cases. | true |
| ShareReferenceFields | bool | Enables reuse of reference type fields regardless of type across cases. | true |
| TagTypeName | string | The name of the enum type generated for the case values. | Case |
| TagPropertyName | string | The name of the property generated to access the case. | Kind |

```CSharp 
    [TypeUnion(GenerateMatch=true, GenerateEquality=true)]
    public partial struct Pet
    {
        public static partial Pet Create(Cat cat);
        public static partial Pet Create(Dog dog);
        public static partial Pet Create(Bird bird);
    }
```

#### You can also customize each case:

In order to customize settings for individual case you can specify a `Case` attribute on the factory method.

| Property | Type | Description | Default |
|:---------|:-----|:------------|:--------|
| Name | string | The name of the case | Infered from factory, type or parameter name |
| TagValue | int | The corresponding enum value for the case | Incrementally generated |
| FactoryName | string | The name of the factory if not declared | 
| FactoryKind | string | The kind of factory generated if not declared; can be Method, Property or None | Method* |
| AccessorName | string | The name of the accessor property | [Case]Value, [Case]Values or Is[Case] |
| AccessorKind | string | The kind of accessor generated; can be Method, Property or None. | Property |
| Type | Type | The type of the case | Inferred from factory |

```CSharp
    [TypeUnion]
    public partial struct Pet
    {
        [Case(Name="C", TagValue=1, AccessorName="CatThings")]
        public static partial Pet Create(AbcCat cat);

        [Case(Name="D", TagValue=2, AccessorName="DogThings")]
        public static partial Pet Create(XyzDog dog);

        [Case(Name="B", TagValue=3, AccessorName="BirdThings")]
        public static partial Pet Create(AcmeBird bird);
    }
```

#### Case Names

The case name is used in the tag enum class and as part of the inferred accessor name.

The generator will try its best to identify the most appropriate name for each case from the factory declaration.
To infer the case name from the factory, the generator follows these steps:

For tag unions:
- If all the factory names are different, the name for each case is the factory name. If all factory names have a common prefix, the prefix is removed.
- Otherwise, the case name is "Case" + n.

For type unions:
- If all the factory names are different, the name for each case is the factory name. If all factory names have a common prefix, the prefix is removed.
- If all parameter names are different, then each case name is inferred from the parameter name.
- If all the case type names are different, then the case name is the name of the case type.
- Otherwise, then the case name is "Type" + n.

In the following example, the generator infers the case names Cat, Dog, Bird from the factory names.
The common prefix of 'Create' is removed.
```CSharp
    [TagUnion]
    public partial struct Pet
    {
        public static partial Pet CreateCat(string name);
        public static partial Pet CreateDog(string name);
        public static partial Pet CreateBird(string name);
    }
```

In this example, the generator infers the case names from the type names
because the factory names and the parameter names are not all different from each other,
but the type names are.
```CSharp
    [TypeUnion]
    public partial struct Pet
    {
        public static partial Pet Create(Cat value);
        public static partial Pet Create(Dog value);
        public static partial Pet Create(Bird value);
    }
```

In this example, the generator infers the case names as Cats, Dogs, Birds from the parameter names because they are all different,
but not all of the types have good unique names.
```CSharp
    [TypeUnion]
    public partial struct Pets
    {
        public static partial Pets Create(IEnumerable<Cat> cats);
        public static partial Pets Create(IEnumerable<Dog> dogs);
        public static partial Pets Create(IEnumerable<Bird> birds);
    }
```

In the final example, the generator gives up and infers the names Type1, Type2, Type3, because neither the factory names, parameter names or type names are all different.
```CSharp
    [TypeUnion]
    public partial struct Pets
    {
        public static partial Pets Create(IEnumerable<Cat> value);
        public static partial Pets Create(IEnumerable<Dog> value);
        public static partial Pets Create(IEnumerable<Bird> value);
    }
```




### Declaring a Case Factory as a Property

If a tag case has no case values you can omit declaring the factory property and instead place a `Case` attribute for it on the union declaration.
When you do so, the factory is generated as a property instead of a method.

```CSharp
    [TagUnion]
    [Case(Name="Unknown")]
    public partial struct Pet
    {
        public static partial Pet Cat(string name, int toys);
        public static partial Pet Dog(string name, bool friendly);
        public static partial Pet Bird(string name, string[] thingsItSays);
    }
```

The new `Unknown` state is now defined as a property.

```CSharp
   Pet pet = Pet.Unknown;
```

You can change how the factory is generated by default with the `FactoryKind` property in the `Case` attribute.

Likewise, if the type used in a type union case is a singleton (a type that only ever has one instance), 
you can have the factory for that case be a property by placing a `Case` attribute on the type union declaration
instead of specifying the factory, and setting the `FactoryKind`.

Normally, if the type used in a type case is a singleton, its factory is still generated by default with a single parameter,
but with `FactoryKind="Property"`, it will be generated as a property.

```CSharp
    // Unknown is a singleton
    public class Unknown 
    { 
        private Union(){} 
        public static readonly Unknown Singleton = new Unknown(); 
    }

    [TypeUnion]
    [Case(Type=typeof(Unknown), FactoryKind="Property")]
    [Case(Type=typeof(Cat))]
    [Case(Type=typeof(Dog))]
    [Case(Type=typeof(Bird))]
    public partial struct Pet
    {
    }
```
Note that even though the factory is a property, you can still assign the singleton value to the union,
since the implicit coercion operator is still being generated.

```CSharp
    Pet pet = Pet.Unknown;  // factory
    Pet pet2 = Unknown.Singleton;  // assignment
```

*For a type to be recognized as a singleton, it must have only private constructors, and declare only one member, a static readonly field that returns an instance of the value.*

### Declaring a Case without an Accessor.

If a case of a type union is a singleton type or a case of a tag union has no values, you can omit the generation of the
accessor property by setting `AccessorKind` in the `Case` attribute.

For example, normally a tag union will generate `Is` properties for cases without values.
When you set the `AccessorKind` to `None`, the `Is` property is not generated.

```CSharp
    [TagUnion]
    [Case(Type=typeof(Cat, AccessorKind="None")]
    public partial struct Pet
    {
        [Case(AccessorKind="None")]
        public static partial Pet Dog();

        public static partial Pet Bird(string name, int numberOfThingsItSays);
    }
```

Now only the bird case as a value accessor, since its the only one with values.

### Assigning Specific Tag Values to Cases.

You may want to avoid versioning problems by future-proofing your union types by assigning them specific tag values for each case.
This is the same as you might do with an enum.

By giving each case a unique value, you can keep some binary compatability with past versions of the union type,
when new cases are added. 

Of course, this won't solve the real problem of code not handling the new cases, 
but at least it keeps the code that does handle the old cases from breaking.

```CSharp
    [TagUnion]
    public partial struct Pet
    {
        [Case(TagValue=1)]
        public static partial Pet Cat(string name, int toys);
        [Case(TagValue=2)]
        public static partial Pet Dog(string name, bool friendly);
        [Case(TagValue=3)]
        public static partial Pet Bird(string name, string[] thingsItSays);
    }
```

*You can do this for both type and tag unions.*

### Declaring a Default Case for a Type or Tag Union

If your custom type or tag union is a struct you may encounter problems that occur when the union is in its default state,
because it is not yet assigned or it was assigned `default`.

You can choose one of your cases to be the default case for this situtation by assigning a `TagValue` of `0`,
since the tag value will otherwise be zero when it is not yet assigned one of the known cases.
>You should only do this only for a value-less tag case or a singleton type case.

*By default, all tag values are assigned postive, non-zero values to avoid being associated with the default state.*

### Generating Union Types without the Toolkit

It is possible to use the generator and not use the toolkit.

Since you wont have access to the `TypeUnion` and `TagUnion` attriutes,
you can place the `@TypeUnion` or `@TagUnion` annotation inside a comment above the partial type declaration.
This is enough to trigger the source generator, but you will only get a type generated with the defaults.

```CSharp
    // @TypeUnion
    public partial struct Pet
    {
        public static partial Pet Create(Cat cat);
        public static partial Pet Create(Dog dog);
        public static partial Pet Create(Bird bird);
    }
```

The ability to customize these may be added in the future.

### Unions with Spans

Custom unions may refer to values of type `Span<T>` or `ReadOnlySpan<T>` or any ref struct type,
as long as the declared union is itself a ref struct type.

```CSharp
    [TypeUnion]
    public ref partial struct RefText
    {
        public static partial RefText Create(ReadOnlySpan<byte> bytes);
        public static partial RefText Create(ReadOnlySpan<char> chars);
    }
```

These unions work with some limitations:
- Type Unions will not implement `ITypeUnion` or generate `TryCreate` or `TryGet` methods.
- Equality methods will not be generated, since the ref types cannot implement `IEquatable<T>` or be boxed.
- ToString will not be generated
- Storage of ref structs is not memory efficient, they will not overlap or decompose, but they may share same type fields across cases.
