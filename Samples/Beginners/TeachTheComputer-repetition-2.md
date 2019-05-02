# Let the computer repeat tasks

Your code should have looked like the following once you finished creating the array of rows: 

``` cs --region more-arrays --source-file .\myapp\PascalsTriangle.cs --project .\myapp\myapp.csproj
```

That's better. Now, let's update this code to make the computers do the repetitive work instead of you. When you copied and pasted the code for each row, you probably did something like the following:

1. Paste the previous row.
1. Add 1 to the index of the row.
1. Copy and paste one element in the array, then increase the column indexes.

Let's make the computer do that for us. Start by removing the statements that initialize the rows of the array. Replace those statements with loop to run for rows 0 through 4:

```csharp
for (int rowIndex = 0; rowIndex < 5; rowIndex++)
{
}
```

Add code to create a new array for the row. Then set the `1` values for the first and last values:

```csharp
triangle[rowIndex] = new int[rowIndex + 1];
triangle[rowIndex][0] = 1;

triangle[rowIndex][rowIndex] = 1;
```

Next, set the columns between the first and last using the previous row. Put the following code where the blank line is in the previous example:

```csharp
for (int column = 1; column < rowIndex; column++)
    triangle[rowIndex][column] = triangle[rowIndex - 1][column - 1] + triangle[rowIndex - 1][column];
```


Try to modify the sample at the top of the page using these instructions. Then continue to see the answer and improve the formatting.

#### Next: [Improve formatting &raquo;](./TeachTheComputer-formatting.md) Previous: [Less repetition &laquo;](./TeachTheComputer-repetition.md) Home: [Home](../README.md)
