// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Pocket;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using WorkspaceServer.Kernel;
using Xunit.Abstractions;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Tests;

namespace WorkspaceServer.Tests.Kernel
{
    public abstract class LanguageKernelTestBase : IDisposable
    {
        protected LanguageKernelTestBase(ITestOutputHelper output)
        {
            DisposeAfterTest(output.SubscribeToPocketLogger());
        }

        private KernelBase CreateLanguageKernel(Language language, Func<INativeAssemblyLoadHelper> getHelper = null)
        {
            var kernelBase = language switch
            {
                Language.FSharp => new FSharpKernel()
                                   .UseDefaultRendering()
                                   .UseKernelHelpers()
                                   .UseDefaultNamespaces() as KernelBase,
                Language.CSharp => new CSharpKernel()
                                   .UseDefaultRendering()
                                   .UseNugetDirective(getHelper)
                                   .UseExtendDirective()
                                   .UseDefaultMagicCommands()
                                   .UseKernelHelpers(),
                _ => throw new InvalidOperationException("Unknown language specified")
            };
            return kernelBase;
        }

        protected KernelBase CreateKernel(Language language, Func<INativeAssemblyLoadHelper> getHelper = null)
        {
            var kernel = CreateLanguageKernel(language, getHelper).LogEventsToPocketLogger();

            KernelEvents = kernel.KernelEvents.ToSubscribedList();

            DisposeAfterTest(KernelEvents);

            return kernel;
        }

        protected KernelBase CreateKernel()
        {
            return CreateKernel(Language.CSharp);
        }

        public async Task<SubmitCode[]> SubmitCode(KernelBase kernel, string[] codeFragments, SubmissionType submissionType = SubmissionType.Run)
        {
            var commands = new List<SubmitCode>();
            foreach (var codeFragment in codeFragments)
            {
                var cmd = new SubmitCode(codeFragment, submissionType: submissionType);
                await kernel.SendAsync(cmd);
                commands.Add(cmd);
            }
            return commands.ToArray();
        }

        public async Task<SubmitCode> SubmitCode(KernelBase kernel, string codeFragment, SubmissionType submissionType = SubmissionType.Run)
        {
            var command = new SubmitCode(codeFragment, submissionType: submissionType);
            await kernel.SendAsync(command);
            return command;
        }

        /// IDispose
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected SubscribedList<IKernelEvent> KernelEvents { get; private set; }

        protected void DisposeAfterTest(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
