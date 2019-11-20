﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelClientTests : IDisposable
    {
        private readonly Configuration _configuration;
        private readonly KernelStreamClient _kernelClient;
        private readonly SubscribedList<JObject> _events;
        private readonly IOStreams _io;
        private readonly ITestOutputHelper _output;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private class IOStreams : IInputTextStream, IOutputTextStream
        {
            private readonly ReplaySubject<string> _input;
            private readonly ReplaySubject<string> _output;

            public IOStreams()
            {
                _input = new ReplaySubject<string>();
                _output = new ReplaySubject<string>();
            }

            public IObservable<string> OutputStream => _output;

            IDisposable IObservable<string>.Subscribe(IObserver<string> observer)
            {
                return _input.Subscribe(observer);
            }

            Task IInputTextStream.Start(CancellationToken token)
            {
                return Task.CompletedTask;
            }

            void IOutputTextStream.Write(string text)
            {
                Task.Run(() => _output.OnNext(text));
            }

            public void WriteToInput(IKernelCommand command, int correlationId)
            {
                var message = ToStreamKernelCommand(command, correlationId);
                _input.OnNext(message.Serialize());
            }

            public static StreamKernelCommand ToStreamKernelCommand(IKernelCommand kernelCommand, int correlationId)
            {
                return new StreamKernelCommand
                {
                    Id = correlationId,
                    CommandType = kernelCommand.GetType().Name,
                    Command = kernelCommand
                };
            }

            public static StreamKernelEvent ToStreamKernelEvent(IKernelEvent kernelEvent, int correlationId)
            {
                return new StreamKernelEvent
                {
                    Id = correlationId,
                    EventType = kernelEvent.GetType().Name,
                    Event = kernelEvent
                };
            }

            public void WriteToInput(string rawMessage)
            {
                Task.Run(() => _input.OnNext(rawMessage));
            }
        }

        public KernelClientTests(ITestOutputHelper output)
        {
            _output = output;
            var displayIdSeed = 0;
            _configuration = new Configuration()
             .UsingExtension("json");

            _configuration = _configuration.SetInteractive(Debugger.IsAttached);

            Microsoft.DotNet.Interactive.Kernel.DisplayIdGenerator =
                () => Interlocked.Increment(ref displayIdSeed).ToString();

            var kernel = new CompositeKernel
            {
                new CSharpKernel()
                    .UseKernelHelpers()
                    .UseNugetDirective()
                    .UseDefaultRendering()
            };

            _io = new IOStreams();

            _kernelClient = new KernelStreamClient(
                kernel,
                _io,
                _io);

            _events = _io.OutputStream
                         .Where(s => !string.IsNullOrWhiteSpace(s))
                         .Select(JObject.Parse)
                         .ToSubscribedList();

            _disposables.Add(_kernelClient);
            _disposables.Add(_output.SubscribeToPocketLogger());
            _disposables.Add(kernel.LogEventsToPocketLogger());
            _disposables.Add(kernel);
            _disposables.Add(() => Microsoft.DotNet.Interactive.Kernel.DisplayIdGenerator = null);
        }

        [Fact]
        public async Task Kernel_can_be_interacted_using_kernel_client()
        {
            await _kernelClient.Start();

            var gate = _io.OutputStream
                .TakeUntilCommandHandled();

            _io.WriteToInput(new SubmitCode(@"var x = 123;"), 0);

            await gate;

            var events = _events
                         .Select(e => e.ToString(Formatting.None));

            var expectedEvents = new List<string> {
                IOStreams.ToStreamKernelEvent(new CommandHandled(new SubmitCode(@"var x = 123;")), 0).Serialize(),
            };

            events.Should()
                .ContainInOrder(expectedEvents);
        }

        [Fact]
        public async Task Kernel_produces_only_commandHandled_for_root_command()
        {
            await _kernelClient.Start();
            var gate = _io.OutputStream
                .TakeUntilCommandHandled();

            _io.WriteToInput(new SubmitCode("display(1543); display(4567);"), 0);

            await gate;

            _events.Should()
                .ContainSingle(e => e["eventType"].Value<string>() == nameof(CommandHandled));
        }

        [Fact]
        public async Task Client_does_not_stream_ReturnValueProduced_events_if_the_value_is_DisplayedValue()
        {
            await _kernelClient.Start();
            var gate = _io.OutputStream
                .TakeUntilCommandHandled();
            _io.WriteToInput(new SubmitCode("display(1543)"), 0);

            await gate;

            _events.Should()
                .NotContain(e => e["eventType"].Value<string>() == nameof(ReturnValueProduced));
        }

        [Fact]
        public async Task Kernel_client_surfaces_json_errors()
        {
            await _kernelClient.Start();
            var gate = _io.OutputStream
                .TakeUntilCommandParseFailure();
            _io.WriteToInput("{ hello");
            
            await gate;

            this.Assent(string.Join("\n", _events.Select(e => e.ToString(Formatting.None))), _configuration);
        }

        [Fact]
        public async Task Kernel_client_surfaces_code_submission_Errors()
        {
            await _kernelClient.Start();
            var gate = _io.OutputStream
                .TakeUntilCommandFailed();

            _io.WriteToInput(new SubmitCode(@"var a = 12"), 0);

            await gate;

            _events.Should()
                .Contain(e => e["eventType"].Value<string>() == nameof(IncompleteCodeSubmissionReceived));
        }

        [Fact]
        public async Task Kernel_client_eval_function_instances()
        {
            await _kernelClient.Start();
            var gate = _io.OutputStream
                .TakeUntilCommandHandled();
            _io.WriteToInput(new SubmitCode(@"Func<int> func = () => 1;"), 0);
            await gate;

            gate = _io.OutputStream
                .TakeUntilEvent<ReturnValueProduced>();
            _io.WriteToInput(new SubmitCode(@"func()"), 1);
            await gate;

            gate = _io.OutputStream
                .TakeUntilEvent<ReturnValueProduced>();
            _io.WriteToInput(new SubmitCode(@"func"), 2);
            await gate;

            await Task.Delay(2.Seconds());
            
            _events
                .Count(e => e["eventType"].Value<string>() == nameof(ReturnValueProduced))
                .Should()
                .Be(2);
        }

        [Fact]
        public async Task Kernel_can_pound_r_nuget_using_kernel_client()
        {
            await _kernelClient.Start();

            var gate = _io.OutputStream
                .TakeUntilEvent<NuGetPackageAdded>(1.Minutes());

            _io.WriteToInput(new SubmitCode(@"#r ""nuget:Microsoft.Spark, 0.4.0"""), 0);

            await gate;

            _events
                .Select(e => e["eventType"].Value<string>())
                .Should()
                .Contain(nameof(NuGetPackageAdded));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}