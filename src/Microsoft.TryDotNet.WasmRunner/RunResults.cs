// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TryDotNet.WasmRunner;

public class RunResults
{
    public RunResults(bool succeeded, Exception? exception = null, Exception? runnerException = null)
    {
        Exception = exception;
        Succeeded = succeeded;
        RunnerException = runnerException;
    }

    public Exception? Exception { get; }
    public bool Succeeded { get; }
    public Exception? RunnerException { get; }
}