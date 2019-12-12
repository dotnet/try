// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;
using Newtonsoft.Json.Linq;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    public class StandardIOKernelServerTests : IDisposable
    {
        private readonly StandardIOKernelServer _standardIOKernelServer;
        private readonly SubscribedList<JObject> _events;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public StandardIOKernelServerTests(ITestOutputHelper output)
        {
            var displayIdSeed = 0;

            Kernel.DisplayIdGenerator =
                () => Interlocked.Increment(ref displayIdSeed).ToString();

            var kernel = new CompositeKernel
            {
                new CSharpKernel()
                    .UseKernelHelpers()
                    .UseNugetDirective()
                    .UseDefaultFormatting()
            };

            _standardIOKernelServer = new StandardIOKernelServer(
                kernel,
                new StreamReader(new MemoryStream()),
                new StringWriter());

            _events = _standardIOKernelServer
                      .Output
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .Select(JObject.Parse)
                      .ToSubscribedList();

            _disposables.Add(_standardIOKernelServer);
            _disposables.Add(output.SubscribeToPocketLogger());
            _disposables.Add(kernel.LogEventsToPocketLogger());
            _disposables.Add(kernel);
            _disposables.Add(() => Kernel.DisplayIdGenerator = null);
        }

        [Fact]
        public async Task It_produces_commandHandled_only_for_root_command()
        {
            await _standardIOKernelServer.WriteAsync(new SubmitCode("display(1543); display(4567);"));

            _events.Should()
                   .ContainSingle(e => e["eventType"].Value<string>() == nameof(CommandHandled));
        }

        [Fact]
        public async Task It_does_not_publish_ReturnValueProduced_events_if_the_value_is_DisplayedValue()
        {
            await _standardIOKernelServer.WriteAsync(new SubmitCode("display(1543)"));

            _events.Should()
                   .NotContain(e => e["eventType"].Value<string>() == nameof(ReturnValueProduced));
        }

        [Fact]
        public async Task It_publishes_diagnostic_events_on_json_parse_errors()
        {
            var invalidJson = "{ hello";

            await _standardIOKernelServer.WriteAsync(invalidJson);

            _events.Should()
                   .ContainSingle<DiagnosticLogEventProduced>()
                   .Which
                   .Message
                   .Should()
                   .Contain(invalidJson);
        }

        [Fact]
        public async Task It_can_surface_code_submission_Errors()
        {
            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"var a = 12"));

            _events.Should().ContainSingle(e => e["eventType"].Value<string>() == nameof(IncompleteCodeSubmissionReceived));
        }

        [Fact]
        public async Task It_can_eval_function_instances()
        {
            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"Func<int> func = () => 1;"), 0);
           
            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"func()"), 1);

            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"func"), 2);

            _events
                .Count(e => e["eventType"].Value<string>() == nameof(ReturnValueProduced))
                .Should()
                .Be(2);
        }

        [Fact]
        public async Task Kernel_can_pound_r_nuget_using_kernel_client()
        {
            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"#r ""nuget:Microsoft.Spark, 0.4.0"""));

            _events
                .Select(e => e["eventType"].Value<string>())
                .Should()
                .Contain(nameof(PackageAdded));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}