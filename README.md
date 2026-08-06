# UnionTypes.Toolkit

Ths repo implements a source generator that generates custom C# union types that use a non-boxing overlapped data strategy.


## Declaring a custom union type

Declare a partial record struct type with a partial 'Cases' method, whose parameters denote the case types for the union.

```csharp
public partial record struct MyUnion
{
    partial void Cases(
        int case1, 
        float case2
        );
}
```

The the implementation of the body of the union type will be generated automatically.

---

## Download the Source Generator

A separate source generator for generating custom union types is also available as a nuget package.  
[Download Generator Here](https://www.nuget.org/packages/UnionTypes.Toolkit.Generators)

---

