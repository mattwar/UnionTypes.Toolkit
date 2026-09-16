# UnionTypes.Toolkit.Generator

A C# source generator library for generating custom union types compatible with the C# union types feature.

It currently implements a single generator that uses techniques to avoid boxing and minimize memory footprint.

The generator is purely standalone; the generated union source does not depend on other libraries at runtime to function.

It may include additional generators in the future.

## Declaring a Non-Boxing Custom Union Type

Declare a partial struct type with `@Union` in its leading comments and a partial constructor for each case type for the generator to implement.

The generator will layout the contents of the custom union so that the fields storing the different case types are overlapped with each other using the same memory space, if possible, or at least consume less space than simply having separate fields for each case type. No case value is boxed.

```csharp
// @union
public partial struct MyUnion
{
    public partial MyUnion(int value);
    public partial MyUnion(float value);
    public partial MyUnion(string value);
    public partial MyUnion(IManifest value);
    public partial MyUnion(Coordinate value);
    public partial MyUnion(Address value);
}

record struct Coordinate(float Longitude, float Latitude);
record struct Address(int Id, string Name);
interface IManifest { ... }
```

In this example, there will be a single field storing a struct that contains enough space to store either an int, float, Coordinate or the address Id and a sparate object field used to store either a string, IManifest or the address Name.

[Learn how to customize the union generation further](https://github.com/mattwar/UnionTypes.Toolkit)
