// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module FSharpMath

[<EntryPoint>]
let main(args: string[]) =
    let mutable acc = 0
    let x = {1 .. 35}
    let a = {1 .. 60}
    let y = {1 .. 30}
    //#region some_region
    let map3 = fun w y z ->
        Seq.map2 (fun x1 (a1,y1) -> (x1,a1,y1)) w (Seq.map2 (fun a1 y1 -> (a1,y1)) y z)
    acc <- (Seq.take 20 (map3 x a y)) |> Seq.fold (fun acc (x, a, y) -> acc + (x + a * y)) 0
    //#endregion
    printfn "%i" acc
    0
