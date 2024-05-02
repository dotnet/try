// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Pocket;

namespace Microsoft.TryDotNet;

internal static class KernelDiagnostics
{
    [DebuggerStepThrough]
    public static T LogCommandsToPocketLogger<T>(this T kernel)
        where T : Kernel
    {
        kernel.AddMiddleware(async (command, context, next) =>
        {
            using var _ = Logger.Log.OnEnterAndExit();
            Logger.Log.Info(command);

            await next(command, context);
        });
        return kernel;
    }

    [DebuggerStepThrough]
    public static T LogEventsToPocketLogger<T>(this T kernel)
        where T : Kernel
    {
        var disposables = new CompositeDisposable();

        kernel.VisitSubkernelsAndSelf(k =>
        {
            disposables.Add(
                k.KernelEvents.Subscribe(e =>
                {
                    Logger.Log.Info("{kernel}: {event} ({details})",
                                        k.Name,
                                        e,
                                        e.ToDisplayString("text/plain"));
                }));
        });

        kernel.RegisterForDisposal(disposables);

        return kernel;
    }
}