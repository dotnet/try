// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System.Threading.Tasks
open FSharp.Compiler.Scripting
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events

type FSharpKernel() =
    inherit KernelBase(Name = "fsharp")
    let script = new FSharpScript()
    do base.AddDisposable(script)
    let handleSubmitCode (codeSubmission: SubmitCode) (context: KernelInvocationContext) =
        async {
            let codeSubmissionReceived = CodeSubmissionReceived(codeSubmission.Code, codeSubmission)
            context.OnNext(codeSubmissionReceived)
            let result, errors =
                try
                    script.Eval(codeSubmission.Code)
                with
                | ex -> Error(ex), [||]
            if errors.Length > 0 then
                let aggregateErrorMessage = System.String.Join("\n", errors)
                context.OnNext(CommandFailed(aggregateErrorMessage, codeSubmission))
            match result with
            | Ok(Some(value)) ->
                let value = value.ReflectionValue
                let formattedValues = FormattedValue.FromObject(value)
                context.OnNext(ReturnValueProduced(value, codeSubmission, formattedValues))
            | Ok(None) -> ()
            | Error(ex) -> context.OnError(ex)
            context.OnNext(CodeSubmissionEvaluated(codeSubmission))
            context.OnCompleted()
        }
    override __.HandleAsync(command: IKernelCommand, _context: KernelInvocationContext): Task =
        async {
            match command with
            | :? SubmitCode as submitCode -> submitCode.Handler <- fun invocationContext -> (handleSubmitCode submitCode invocationContext) |> Async.StartAsTask :> Task
            | _ -> ()
        } |> Async.StartAsTask :> Task

