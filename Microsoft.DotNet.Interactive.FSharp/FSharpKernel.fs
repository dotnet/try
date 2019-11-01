// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.Collections.Generic
open System.IO
open System.Threading
open System.Threading.Tasks
open FSharp.Compiler.Interactive.Shell
open FSharp.Compiler.Scripting
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open MLS.Agent.Tools

type FSharpKernel() =
    inherit KernelBase(Name = "fsharp")

    let resolvedAssemblies = List<string>()
    let script = new FSharpScript(additionalArgs=[|"/langversion:preview"|])
    let mutable cancellationTokenSource = new CancellationTokenSource()

    do Event.add resolvedAssemblies.Add script.AssemblyReferenceAdded
    do base.AddDisposable(script)

    let handleSubmitCode (codeSubmission: SubmitCode) (context: KernelInvocationContext) =
        async {
            let codeSubmissionReceived = CodeSubmissionReceived(codeSubmission.Code, codeSubmission)
            context.Publish(codeSubmissionReceived)
            use! console = ConsoleOutput.Capture() |> Async.AwaitTask
            use _ = console.SubscribeToStandardOutput(fun msg -> context.Publish(StandardOutputValueProduced(msg, codeSubmission, FormattedValue.FromObject(msg))))
            use _ = console.SubscribeToStandardError(fun msg -> context.Publish(StandardErrorValueProduced(msg, codeSubmission, FormattedValue.FromObject(msg))))
            resolvedAssemblies.Clear()
            let tokenSource = cancellationTokenSource
            let result, errors =
                try
                    script.Eval(codeSubmission.Code, tokenSource.Token)
                with
                | ex -> Error(ex), [||]

            match result with
            | Ok(result) ->
                match result with
                | Some(value) ->
                    let value = value.ReflectionValue
                    let formattedValues = FormattedValue.FromObject(value)
                    context.Publish(ReturnValueProduced(value, codeSubmission, formattedValues))
                | None -> ()
                context.Complete()
            | Error(ex) ->
                if not (tokenSource.IsCancellationRequested) then
                    let aggregateError = String.Join("\n", errors)
                    let reportedException =
                        match ex with
                        | :? FsiCompilationException -> CodeSubmissionCompilationErrorException(ex) :> Exception
                        | _ -> ex
                    context.Publish(CommandFailed(reportedException, codeSubmission, aggregateError))
                else
                    context.Publish(new CommandFailed(null, codeSubmission, "Command cancelled"))
        }

    let handleCancelCurrentCommand (cancelCurrentCommand: CancelCurrentCommand) (context: KernelInvocationContext) =
        async {
            cancellationTokenSource.Cancel()
            cancellationTokenSource.Dispose()
            cancellationTokenSource <- new CancellationTokenSource()
            context.Publish(CurrentCommandCancelled(cancelCurrentCommand))
        }

    override __.HandleAsync(command: IKernelCommand, _context: KernelInvocationContext): Task =
        async {
            match command with
            | :? SubmitCode as submitCode -> submitCode.Handler <- fun invocationContext -> (handleSubmitCode submitCode invocationContext) |> Async.StartAsTask :> Task
            | :? CancelCurrentCommand as cancelCurrentCommand -> cancelCurrentCommand.Handler <- fun invocationContext -> (handleCancelCurrentCommand cancelCurrentCommand invocationContext) |> Async.StartAsTask :> Task
            | _ -> ()
        } |> Async.StartAsTask :> Task
