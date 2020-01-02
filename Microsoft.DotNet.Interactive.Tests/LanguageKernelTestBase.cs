// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using Xunit.Abstractions;
using Serilog.Sinks.RollingFileAlternate;
using Xunit.Sdk;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace Microsoft.DotNet.Interactive.Tests
{
    internal class LogTestNamesToPocketLoggerAttribute : BeforeAfterTestAttribute
    {
        private static readonly ConcurrentDictionary<MethodInfo, OperationLogger> _operations = new ConcurrentDictionary<MethodInfo, OperationLogger>();

        public override void Before(MethodInfo methodUnderTest)
        {
            var x = Logger.Log.OnEnterAndExit(name: methodUnderTest.Name);
            _operations.TryAdd(methodUnderTest, x);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (_operations.TryRemove(methodUnderTest, out var operation))
            {
                operation.Dispose();
            }
        }
    }

    [LogTestNamesToPocketLogger]
    public abstract class LanguageKernelTestBase : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        static LanguageKernelTestBase()
        {
            var artifactsPath = new DirectoryInfo(".");

            while (artifactsPath.Name != "artifacts" && artifactsPath != artifactsPath.Root)
            {
                artifactsPath = artifactsPath.Parent;
            }

            var logPath = Path.Combine(
                artifactsPath.ToString(),
                "log",
                "Release");

            var log = new SerilogLoggerConfiguration()
                      .WriteTo
                      .RollingFileAlternate(logPath, outputTemplate: "{Message}{NewLine}")
                      .CreateLogger();

            LogEvents.Subscribe(
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
