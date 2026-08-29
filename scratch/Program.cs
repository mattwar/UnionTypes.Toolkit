var u = new MyUnion(new A(1, 2.0f));

Console.WriteLine(u);

if (u is A)
   Console.WriteLine("I'm an A");

if (u is IFoo)
   Console.WriteLine("I'm an IFoo");

if (u is IBar)
   Console.WriteLine("I'm an IBar");

if (u is int)
   Console.WriteLine("I'm an int");

public partial struct MyUnion
{
    partial void Cases(
        int x,
        IBar bar,
        IFoo foo,
        A a
        );
}

public interface IFoo {}
public interface IBar {}
public record struct A(int X, float Y) : IFoo;


