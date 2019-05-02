# Let the computer repeat tasks

Your hand coded solution should look something like the following code: 

``` cs --region handcoded-answer --source-file .\myapp\PascalsTriangle.cs --project .\myapp\myapp.csproj
```

As you put together that solution, you likely found yourself copying, pasting, and modifying some of the code. That is often a great way to explore a problem and try different ways to solve it. Once you understand the problem and have found a good solution, you can improve the code so you don't copy and past similar statements. Instead, let the computer repeat those steps.

You'll make these changes in two steps. In the first step, you'll make a collection for all the rows in the triangle. Instead of the variables `row0`, `row1`, and so on, declare an array of arrays like the following:

```csharp
int[][] triangle = new int[5][];
```

From that, you can replace any use of `row0` with `triangle[0]`, `row1` with `triangle[1]` and so on. You'll also remove the declarations of `int[]` before `row0`, `row1` and so on. The variable `triangle` already creates the definitions for those types. As you start replacing variables, you'll see red squiggles highlighting where you use the variables you're replacing. After those replacements, you can replace the code that writes the values with one loop:

```csharp
foreach (int[] row in triangle)
{
      foreach (var item in row)
         Console.Write(item + " ");
      Console.WriteLine();
}
```

Try to modify the sample at the top of the page using these instructions. Run when you don't have red squiggles under your code. If the output window turns red, you may have the wrong indices in your array. Once you've got this working,  continue for a bit more changes.

#### Next: [Build the triangle &raquo;](./TeachTheComputer-repetition-2.md)     Previous: [Teach the computer &laquo;](./TeachTheComputer.md)      Home: [Home](../README.md)
