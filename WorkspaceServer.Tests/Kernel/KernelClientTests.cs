// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Newtonsoft.Json.Linq;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public class KernelClientTests
    {

        private readonly Configuration _configuration;

        private MemoryStream _input;
        private MemoryStream _output;

        public KernelClientTests()
        {
            _configuration = new Configuration()
                .UsingExtension("json");
            _configuration = _configuration.SetInteractive(Debugger.IsAttached);


        }

        private KernelStreamClient CreateClient(IKernel kernel = null)
        {
            kernel ??= new CompositeKernel
            {
                new CSharpKernel()
                    .UseKernelHelpers()
                    .UseNugetDirective()
                    .UseDefaultRendering()
            };

            _input = new MemoryStream();

            _output = new MemoryStream();

            return new KernelStreamClient(kernel,
                new StreamReader(_input),
                new StreamWriter(_output));
        }

        private void SendOnClient(params IKernelCommand[] commands)
        {
            var writer = new StreamWriter(_input, Encoding.UTF8);
            for (var i = 0; i < commands.Length; i++)
            {
                writer.WriteMessage(commands[i], i);
            }

            writer.Flush();
            _input.Position = 0;
        }

        private void SendOnClient(string rawMessage, params IKernelCommand[] commands)
        {
            var writer = new StreamWriter(_input, Encoding.UTF8);
            writer.WriteLine(rawMessage);
            for (var i = 0; i < commands.Length; i++)
            {
                writer.WriteMessage(commands[i], i);
            }

            writer.Flush();
            _input.Position = 0;
        }

        private string ReadAllOutput()
        {
            _output.Position = 0;
            var reader = new StreamReader(_output, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private IEnumerable<JObject> ReadAllOutputAsJson()
        {
            var text = ReadAllOutput();
            return text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(JObject.Parse).ToList();
        }

        [Fact]
        public async Task Kernel_can_be_interacted_using_kernel_client()
        {
            var streamKernel = CreateClient();
            SendOnClient(
                new SubmitCode(@"var x = 123;"),
                new SubmitCode("display(x); display(x + 1); display(x + 2);"),
                new Quit());

            await streamKernel.Start();

            this.Assent(ReadAllOutput(), _configuration);
        }


        [Fact]
        public async Task Kernel_produces_only_commandHandled_for_root_command()
        {
            var streamKernel = CreateClient();
            SendOnClient(
                new SubmitCode("display(1543); display(4567);"),
                new Quit());

            await streamKernel.Start();

            var events = ReadAllOutputAsJson();
            events.Should()
                .ContainSingle(e => e["eventType"].Value<string>() == nameof(CommandHandled));
        }

        [Fact]
        public async Task Kernel_client_surfaces_json_errors()
        {
           var streamKernel = CreateClient(new FakeKernel("Fake"));
            SendOnClient("{ hello"
                , new Quit());
            var task = streamKernel.Start();
            await task;

            var text = ReadAllOutput();
            this.Assent(text, _configuration);
        }

        [Fact]
        public async Task Kernel_client_surfaces_code_submission_Errors()
        {
            var streamKernel = CreateClient();
            SendOnClient(new SubmitCode(@"var a = 12"),
                new Quit());

            await streamKernel.Start();

            var events = ReadAllOutputAsJson();

            events.Should()
                .Contain(e => e["eventType"].Value<string>() == nameof(IncompleteCodeSubmissionReceived))
                .And
                .Contain(e => e["eventType"].Value<string>() == nameof(CommandFailed));
        }

        [Fact]
        public async Task Kernel_client_surfaces_kernelBusy_and_kernelIdle_events()
        {
            var streamKernel = CreateClient();
            SendOnClient(
                new SubmitCode(@"var a = 12;"),
                new SubmitCode(@"var b = 12;"),
            new Quit());

            await streamKernel.Start();

            var events = ReadAllOutputAsJson();

            events.Select(e => e["eventType"].Value<string>())
                .Should()
                .ContainSingle(e => e == nameof(KernelBusy))
                .And
                .ContainSingle(e => e == nameof(KernelBusy))
                .And
                .StartWith(nameof(KernelBusy))
                .And
                .EndWith(nameof(KernelIdle));
        }

        [Fact]
        public async Task Kernel_can_pound_r_nuget_using_kernel_client()
        {
            var streamKernel = CreateClient();
            SendOnClient(new SubmitCode(@"#r ""nuget:Microsoft.Spark, 0.4.0"""),
                new Quit());

            await streamKernel.Start();
            var events = ReadAllOutputAsJson();

            events.Should().Contain(e => e["eventType"].Value<string>() == nameof(NuGetPackageAdded));
        }
    }
}