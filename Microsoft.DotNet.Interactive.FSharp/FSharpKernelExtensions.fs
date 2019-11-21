namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Html
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.FSharp
open Microsoft.DotNet.Interactive.Formatting
open XPlot.Plotly

[<AbstractClass; Extension; Sealed>]
type FSharpKernelExtensions private () =
    
    static let referenceFromType = fun (typ: Type) -> sprintf "#r \"%s\"" (typ.Assembly.Location.Replace("\\", "/"))
    static let openNamespaceOrType = fun (whatToOpen: String) -> sprintf "open %s" whatToOpen

    [<Extension>]
    static member UseDefaultFormatting(kernel: FSharpKernel) = 
        let t = 
            async {
                let code = 
                    [
                        referenceFromType typeof<IHtmlContent>
                        referenceFromType typeof<IKernel>
                        referenceFromType typeof<FSharpPocketViewTags>
                        referenceFromType typeof<PlotlyChart>
                        referenceFromType typeof<Formatter>
                        openNamespaceOrType typeof<IHtmlContent>.Namespace
                        openNamespaceOrType typeof<FSharpPocketViewTags>.FullName
                        openNamespaceOrType typeof<FSharpPocketViewTags>.Namespace
                        openNamespaceOrType typeof<PlotlyChart>.Namespace
                        openNamespaceOrType typeof<Formatter>.Namespace
                    ] |> List.reduce(fun x y -> x + Environment.NewLine + y)

                return! kernel.SendAsync(SubmitCode code) |> Async.AwaitTask
            } 
        Async.RunSynchronously t |> ignore
        kernel

    [<Extension>]
    static member UseDefaultNamespaces(kernel: FSharpKernel) =
        let t = 
            async {
                let code = @"
open System
open System.Text
open System.Threading.Tasks
open System.Linq
                "
                return! kernel.SendAsync(SubmitCode code) |> Async.AwaitTask
            }
        Async.RunSynchronously t |> ignore
        kernel

    [<Extension>]
    static member UseKernelHelpers(kernel: FSharpKernel) =
        let t = 
            async {
                let code = openNamespaceOrType (typeof<FSharpKernelHelpers.IMarker>.DeclaringType.Namespace + "." + nameof(FSharpKernelHelpers))
                return! kernel.SendAsync(SubmitCode code) |> Async.AwaitTask
            }
        Async.RunSynchronously t |> ignore
        kernel