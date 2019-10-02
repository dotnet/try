// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System.Collections.Generic
open System.IO
open System.Threading.Tasks
open FSharp.Compiler.Scripting
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open MLS.Agent.Tools

type FSharpKernel() as this =
    inherit KernelBase(Name = "fsharp")
    let resolvedAssemblies = List<string>()
    let script = new FSharpScript(additionalArgs=[|"/langversion:preview"|])

    do Event.add resolvedAssemblies.Add script.AssemblyReferenceAdded
    do base.AddDisposable(script)

    let handleAssemblyReferenceAdded path context =
        async {
            let loader = KernelExtensionLoader()
            let fileInfo = FileInfo(path)
            let! success = loader.LoadFromAssembly(fileInfo, this, context) |> Async.AwaitTask
            return success
        }

    let handleSubmitCode (codeSubmission: SubmitCode) (context: KernelInvocationContext) =
        async {
            let codeSubmissionReceived = CodeSubmissionReceived(codeSubmission.Code, codeSubmission)
            context.Publish(codeSubmissionReceived)
            use! console = ConsoleOutput.Capture() |> Async.AwaitTask
            use _ = console.SubscribeToStandardOutput(fun msg -> context.Publish(StandardOutputValueProduced(msg, codeSubmission, FormattedValue.FromObject(msg))))
            use _ = console.SubscribeToStandardError(fun msg -> context.Publish(StandardErrorValueProduced(msg, codeSubmission, FormattedValue.FromObject(msg))))
            resolvedAssemblies.Clear()
            let result, errors =
                try
                    script.Eval(codeSubmission.Code)
                with
                | ex -> Error(ex), [||]
            if errors.Length > 0 then
                let aggregateErrorMessage = System.String.Join("\n", errors)
                context.Publish(CommandFailed(aggregateErrorMessage, codeSubmission))
            for asm in resolvedAssemblies do
                let! _success = handleAssemblyReferenceAdded asm context
                () // don't care
            match result with
            | Ok(Some(value)) ->
                let value = value.ReflectionValue
                let formattedValues = FormattedValue.FromObject(value)
                context.Publish(ReturnValueProduced(value, codeSubmission, formattedValues))
            | Ok(None) -> ()
            | Error(ex) -> context.OnError(ex)
            context.Publish(CommandHandled(codeSubmission))
        }

    let handleCancelCurrentCommand (cancelCurrentCommand: CancelCurrentCommand) (context: KernelInvocationContext) =
        async {
            let reply = CurrentCommandCancelled(cancelCurrentCommand)
            context.Publish(reply)
        }

    override __.HandleAsync(command: IKernelCommand, _context: KernelInvocationContext): Task =
        async {
            match command with
            | :? SubmitCode as submitCode -> submitCode.Handler <- fun invocationContext -> (handleSubmitCode submitCode invocationContext) |> Async.StartAsTask :> Task
            | :? CancelCurrentCommand as cancelCurrentCommand -> cancelCurrentCommand.Handler <- fun invocationContext -> (handleCancelCurrentCommand cancelCurrentCommand invocationContext) |> Async.StartAsTask :> Task
            | _ -> ()
        } |> Async.StartAsTask :> Task
