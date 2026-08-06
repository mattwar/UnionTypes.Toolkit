var u = new MyUnion(10.0f);
Console.WriteLine(u.Value);

public partial struct MyUnion
{
    partial void Cases(int value1, float value2);
}
