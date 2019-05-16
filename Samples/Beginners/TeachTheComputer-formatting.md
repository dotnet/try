# Let the computer repeat tasks

Your code should have looked like the following once you finished updating the initialization:

``` cs --region initialize-in-loop --source-file .\myapp\PascalsTriangle.cs --project .\myapp\myapp.csproj
```

That's better. The last step is to improve the formatting.  Replace the existing loop to print the rows of the triangle with the following code:

```csharp
const int fieldWidth = 6;
foreach (int[] row in triangle)
{
    int indent = (5- row.Length) * (fieldWidth / 2);
    Console.Write(new string(' ', indent));
    foreach (var item in row)
        Console.Write($"{item, fieldWidth}");
    Console.WriteLine();
}
```

This final version makes it easy to build more and more rows.  You may notice the number `5` in many places in this code. You can declare a constant instead.:

```csharp
const int MaxRows = 5;
```

Then, replace all instances of `5` with `MaxRows`. Now, if you change `MaxRows`, you'll generate however many rows you want!. Well, within reason. In this environment, your program will time out if go too far.

You've finished this tutorial. You've taught the computer to do some of your math homework.

#### Previous: [Build the triangle &laquo;](./TeachTheComputer-repetition-2.md) Home: [Home](../Readme.md)
