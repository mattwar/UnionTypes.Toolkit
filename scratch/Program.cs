using System.Runtime.CompilerServices;

MyUnion u = new A(1, 2);

Console.WriteLine($"size: {Unsafe.SizeOf<MyUnion>()}");

if (u is byte)
   Console.WriteLine("I'm a byte");

if (u is IFoo)
   Console.WriteLine("I'm an IFoo");

if (u is IBar)
   Console.WriteLine("I'm an IBar");

if (u is int)
   Console.WriteLine("I'm an int");

// @union
public partial struct MyUnion
{
   public partial MyUnion(int value);
   public partial MyUnion(IBar bar);
   public partial MyUnion(IFoo foo);
   public partial MyUnion(byte y);
}

public interface IFoo {}
public interface IBar {}
public record struct A(int X, byte Y) : IFoo;


