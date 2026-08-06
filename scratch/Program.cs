var u = new MyUnion(10);
Console.WriteLine(u);

public partial struct MyUnion
{
    partial void Cases(int value1, float value2);
}
