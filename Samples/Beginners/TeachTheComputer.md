# Teach the computer

As you learn more about programming, you'll find that you can teach the computer to do more and more work for you. Let's use an example you may recognize from math class. Try this even if math isn't your favorite class. You'll learn to get the computer to do your work for you.

Pascal's triangle is a visualization of math pattern. The first row is a single `1`. The following rows all have one more item than the previous. Each row is created by adding `1` as the first and last element. Each inner element is found by taking the sum of the two numbers above directly to the left and right. You can see this in the row `1 2 1`. The `2` is the sum of the two `1`s above. Similarly, the `1 3 3 1` row: The first `3` is from the `1 2` in the row above, and next `3` is from the `2 1` that ends the row above.

```console
                                                     1
                                                  1     1
                                               1     2     1
                                            1     3     3     1
                                         1     4     6     4     1
                                      1     5    10    10     5     1
                                   1     6    15    20    15     6     1
                                1     7    21    35    35    21     7     1
                             1     8    28    56    70    56    28     8     1
                          1     9    36    84   126   126    84    36     9     1
                       1    10    45   120   210   252   210   120    45    10     1
                    1    11    55   165   330   462   462   330   165    55    11     1
                 1    12    66   220   495   792   924   792   495   220    66    12     1
              1    13    78   286   715  1287  1716  1716  1287   715   286    78    13     1
           1    14    91   364  1001  2002  3003  3432  3003  2002  1001   364    91    14     1
        1    15   105   455  1365  3003  5005  6435  6435  5005  3003  1365   455   105    15     1
     1    16   120   560  1820  4368  8008 11440 12870 11440  8008  4368  1820   560   120    16     1
```

Computing successive rows in the triangle is not hard. But, it's very repetitive. Computers are great at tasks like these. You can teach it!

## Explore code

A good way to teach the computer is to start by exploring the problem yourself. The first step is to create some simple code that generates the first few rows. Try the following code yourself. It uses some of the concepts you've already learned. This example creates an array for each row of elements. It initializes the elements in the array. The last part prints the values in each row.

``` cs --region handcoded --source-file .\myapp\PascalsTriangle.cs --project .\myapp\myapp.csproj
```

The format of the output doesn't form a nice triangle yet. Let's concentrate on the values first. Try and add a couple more rows to the triangle. Copy and paste the `row2` line and add rows 3 and 4. Do the same with the `foreach` loops that write the values.

#### Next: [Less repetition &raquo;](./TeachTheComputer-repetition.md) Home: [Home](../Readme.md)
