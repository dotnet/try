// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Extensions;
using Pocket;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensions
    {
        public static Task<IKernelCommandResult> SendAsync(
            this IKernel kernel,
            IKernelCommand command)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(command, CancellationToken.None);
        }

        public static Task<IKernelCommandResult> SubmitCodeAsync(
            this IKernel kernel, 
            string code)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(new SubmitCode(code), CancellationToken.None);
        }

        public static T UseExtendDirective<T>(this T kernel)
            where T : KernelBase
        {
            var extensionDllArg = new Argument<FileInfo>("dll")
                .ExistingOnly();

            var extend = new Command("#extend")
            {
                extensionDllArg
            };

            extend.Handler = CommandHandler.Create<FileInfo, KernelInvocationContext>(
                (dll, pipelineContext) =>
                    kernel.SendAsync(new LoadExtension(dll)));

            kernel.AddDirective(extend);

            return kernel;
        }

        public static T LogEventsToPocketLogger<T>(this T kernel)
            where T : IKernel
        {
            var disposables = new CompositeDisposable();

            disposables.Add(
                kernel.KernelEvents
                      .Subscribe(
                          e =>
                          {
                              Logger.Log.Info("{kernel}: {event}",
                                              kernel.Name,
                                              e);
                          }));

            kernel.VisitSubkernels(k =>
            {
                disposables.Add(
                    k.KernelEvents.Subscribe(
                        e =>
                        {
                            Logger.Log.Info("{kernel}: {event}",
                                            k.Name,
                                            e);
                        }));
            });

            return kernel;
        }
    }
}