# UnionTypes.Toolkit.Generators

A C# source generator library for generating custom union types compatible with the C# union types feature.

It currently implements a single generator that uses techniques to avoid boxing and minimize memory footprint.

The generator is purely standalone; the generated union source does not depend on other libraries at runtime to function.

It may include additional generators in the future.

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

The generator will layout the contents of the custom union so that the fields storing the different case types are overlapped with each other using the same memory space, if possible, or at least consume less space than simply having separate fields for each case type. No case value is boxed.

In this example, there will be a single field storing a struct that contains enough space to store either an int, float, Coordinate or the address Id and a sparate object field used to store either a string, IManifest or the address Name.

# [Learn how to customize the union generation further](https://github.com/mattwar/UnionTypes.Toolkit)
