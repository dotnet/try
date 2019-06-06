# dotnet try

Learn math with .NET.

Given the following mathematical expression 

$$
\sum ^{20}_{i=0}\left(x_{i}+a_{i}y_{i}\right)
$$

Create an implementation using `F#`. The sequences `x`, `y` and `a` have been already declared.

```fsharp --source-file ./samples/FSharpMath/Program.fs --project ./samples/FSharpMath/FSharpMath.fsproj  --region some_region
let map3 = fun w y z ->
    Seq.map2 (fun x1 (a1,y1) -> (x1,a1,y1)) w (Seq.map2 (fun a1 y1 -> (a1,y1)) y z)
acc <- (Seq.take 20 (map3 x a y)) |> Seq.fold (fun acc (x, a, y) -> acc + (x + a * y)) 0
```
