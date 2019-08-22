// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.ComponentModel
open System.Diagnostics
open System.Text
open System.Threading.Tasks
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open MLS.Agent.Tools

type FSharpKernel() =
    inherit KernelBase()
    let mutable proc: Process = null
    let write (s: string) = proc.StandardInput.Write(s)
    let sentinelValue = Guid.NewGuid().ToString()
    let sentinelFound = Event<_>()
    let stdout = StringBuilder()
    let waitForReady () =
        async {
            write <| sprintf ";;printfn \"\\n%s\";;\n" sentinelValue
            do! Async.AwaitEvent sentinelFound.Publish
        }
    let startProcess () =
        async {
            if isNull proc then
                let outputReceived line =
                    if line = sentinelValue then
                        sentinelFound.Trigger()
                    else
                        stdout.AppendLine(line) |> ignore
                proc <- Dotnet().StartProcess("fsi --nologo", output = Action<string>(outputReceived))
                do! waitForReady()
        }
    let eval (code: string) =
        async {
            do! startProcess ()
            stdout.Clear() |> ignore
            write code
            do! waitForReady ()
            let value = stdout.ToString()
            // trim garbage
            let nl = Environment.NewLine
            let headerGarbage = sprintf "val it : unit = ()%s%s" nl nl
            let value =  if value.StartsWith(headerGarbage) then value.Substring(headerGarbage.Length) else value
            let footerGarbage = sprintf "%s%s> %s" nl nl nl
            let value = if value.EndsWith(footerGarbage) then value.Substring(0, value.Length - footerGarbage.Length) else value
            return value
        }
    do base.AddDisposable({ new IDisposable with
                                member __.Dispose() =
                                    if not <| isNull proc then
                                        try
                                            proc.Kill()
                                        with
                                        | :? InvalidOperationException -> ()
                                        | :? NotSupportedException -> ()
                                        | :? Win32Exception -> () })
    let handleSubmitCode (codeSubmission: SubmitCode) (context: KernelInvocationContext) =
        async {
            let codeSubmissionReceived = CodeSubmissionReceived(codeSubmission.Code, codeSubmission)
            context.OnNext(codeSubmissionReceived)
            // submit code
            let! value = eval codeSubmission.Code
            context.OnNext(ValueProduced(value, codeSubmission, true, [FormattedValue("text/plain", value)]))
            context.OnNext(CodeSubmissionEvaluated(codeSubmission))
            context.OnCompleted()
        }
    override __.Name = "fsharp"
    override __.HandleAsync(command: IKernelCommand, _context: KernelInvocationContext): Task =
        async {
            match command with
            | :? SubmitCode as submitCode -> submitCode.Handler <- fun invocationContext -> (handleSubmitCode submitCode invocationContext) |> Async.StartAsTask :> Task
            | _ -> ()
        } |> Async.StartAsTask :> Task
