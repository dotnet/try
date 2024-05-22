// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.JSInterop;

namespace Microsoft.TryDotNet.WasmRunner;

internal class CodeRunnerAdapter
{
    private readonly CodeRunner _runner;
    private readonly IJSRuntime _jsInterop;

    public CodeRunnerAdapter(CodeRunner runner, IJSRuntime jsInterop)
    {
        _runner = runner;
        _jsInterop = jsInterop;
    }

    [JSInvokable]
    public async Task<SerializableCodeRunnerResult> RunAssembly(string base64EncodedAssembly)
    {
        var result = await _runner.RunAssemblyEntryPoint(base64EncodedAssembly,
            output => _jsInterop.InvokeAsync<object>("publishCodeRunnerStdOut", output),
            error => _jsInterop.InvokeAsync<object>("publishCodeRunnerStdError", error)
            );

        return new SerializableCodeRunnerResult(result.Succeeded, result.Exception?.ToString(), result.RunnerException?.ToString());
    }
}

internal record SerializableCodeRunnerResult(bool Success, string? Error, string? RunnerError);