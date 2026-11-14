using System.Runtime.CompilerServices;

DisplayUnion(default);
DisplayUnion(123);
DisplayUnion("Hello");

// this will succeed because type parameter case did not generate null protection,
// because type of case is only known at runtime and runtime cannot know about nullability of type parameters
var y = new Union<string, int>((string)null!);

// this will throw on purpose because MyUnion does not allow null for valid state
//var x = new MyUnion((string)null!);


void DisplayUnion(MyUnion u)
{
   Console.WriteLine($"HasValue: {u.HasValue}");
   Console.WriteLine($"Value: {u.Value}");

   switch (u)
   {
      case int intVal:
         Console.WriteLine($"is int: {intVal}");
         break;
      case string strVal:
         Console.WriteLine($"is string: {strVal}");
         break;
      case null:
         Console.WriteLine("is null");
         break;
   }

   Console.WriteLine();
}

// @union
public partial struct MyUnion
{
   public partial MyUnion(int value);
   public partial MyUnion(string value);
   private static MyUnion Default { get; } = -999;
}


// @union
public partial struct Union<T1, T2>
{
   public partial Union(T1 value);
   public partial Union(T2 value);
   public partial Union(float value);
}
