﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Pocket;
using Xunit.Abstractions;
using Serilog.Sinks.RollingFileAlternate;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace Microsoft.DotNet.Interactive.Tests
{
    public abstract class LanguageKernelTestBase : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        static LanguageKernelTestBase()
        {
                var log = new SerilogLoggerConfiguration()
                          .WriteTo
                          .RollingFileAlternate(".", outputTemplate: "{Message}{NewLine}")
                          .CreateLogger();

                var subscription = LogEvents.Subscribe(
                    e => log.Information(e.ToLogString()));
        }

        protected LanguageKernelTestBase(ITestOutputHelper output)
        {
            DisposeAfterTest(output.SubscribeToPocketLogger());
        }

        protected KernelBase CreateKernel(Language language)
        {
            var kernelBase = language switch
            {
                Language.FSharp => new FSharpKernel()
                                   .UseDefaultFormatting()
                                   .UseKernelHelpers()
                                   .UseDefaultNamespaces() as KernelBase,
                Language.CSharp => new CSharpKernel()
                                   .UseDefaultFormatting()
                                   .UseNugetDirective()
                                   .UseKernelHelpers()
                                   .UseWho(),
                _ => throw new InvalidOperationException("Unknown language specified")
            };

            var languageSpecificKernel = kernelBase
                         .UseDefaultMagicCommands()
                         .UseExtendDirective()
                         .LogEventsToPocketLogger();

            KernelEvents = languageSpecificKernel.KernelEvents.ToSubscribedList();

            DisposeAfterTest(languageSpecificKernel);

            return languageSpecificKernel;
        }

        protected KernelBase CreateKernel()
        {
            return CreateKernel(Language.CSharp);
        }

        public async Task SubmitCode(KernelBase kernel, string[] codeFragments, SubmissionType submissionType = SubmissionType.Run)
        {
            foreach (var codeFragment in codeFragments)
            {
                var cmd = new SubmitCode(codeFragment, submissionType: submissionType);
                await kernel.SendAsync(cmd);
            }
        }

        public async Task SubmitCode(KernelBase kernel, string codeFragment, SubmissionType submissionType = SubmissionType.Run)
        {
            var command = new SubmitCode(codeFragment, submissionType: submissionType);
            await kernel.SendAsync(command);
        }

        protected SubscribedList<IKernelEvent> KernelEvents { get; private set; }

        protected void DisposeAfterTest(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public void Dispose() => _disposables?.Dispose();
    }
}
