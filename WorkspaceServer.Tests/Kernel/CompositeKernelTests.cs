﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorkspaceServer.Tests.Kernel
{
    public class CompositeKernelTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public CompositeKernelTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact(Skip = "WIP")]
        public void When_SubmitCode_command_adds_packages_to_fsharp_kernel_then_the_submission_is_passed_to_fsi()
        {
            // FIX: move to FSharpKernelTests
            throw new NotImplementedException();
        }

        [Fact(Skip = "WIP")]
        public void When_SubmitCode_command_adds_packages_to_fsharp_kernel_then_PackageAdded_event_is_raised()
        {
            // FIX: move to FSharpKernelTests
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_magic_command()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            await kernel.SendAsync(
                new SubmitCode(
                    @"%%csharp
var x = 123;"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"%%fake
hello!"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"%%csharp
x"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_by_setting_the_kernel_name_in_the_command()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            await kernel.SendAsync(
                new SubmitCode(
                    @"var x = 123;",
                    "csharp"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!",
                    "fake"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"x",
                    "csharp"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_in_middleware()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            kernel.Pipeline.AddMiddleware(async (command, context, next) =>
            {
                context.HandlingKernel = kernel.ChildKernels.Single(k => k.Name == "fake");
                await next(command, context);
            });

            await kernel.SendAsync(new SubmitCode("hello!"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_as_a_default()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            kernel.DefaultKernelName = "fake";

            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Kernel_client_surfaces_json_errors()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                     Handle = context =>
                    {
                        return Task.CompletedTask;
                    }
                }
            };

            var input = new MemoryStream();
            var writer = new StreamWriter(input, Encoding.UTF8);
            writer.WriteLine("{ hello");
            writer.WriteMessage(new Quit());
            writer.Flush();

            input.Position = 0;

            var output = new MemoryStream();

            var streamKernel = new KernelStreamClient(kernel,
                new StreamReader(input),
                new StreamWriter(output));

            var task = streamKernel.Start();
            await task;

            output.Position = 0;
            var reader = new StreamReader(output, Encoding.UTF8);

            var text = reader.ReadToEnd();
            var events = text.Split(Environment.NewLine)
                .Select(e => JsonConvert.DeserializeObject<StreamKernelEvent>(e));

            events.Should().Contain(e => e.EventType == "CommandParseFailure");
        }

        [Fact]
        public async Task Kernel_can_pound_r_nuget_using_kernel_client()
        {
              var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
            };

            var test =  JsonConvert.SerializeObject(new SubmitCode(@"#r nuget:""Microsoft.Extensions.Logging"""));

            var input = new MemoryStream();
            var writer = new StreamWriter(input, Encoding.UTF8);
            writer.WriteMessage(new SubmitCode(@"#r nuget:""Microsoft.Spark, 0.4.0"""));
            writer.WriteMessage(new Quit());

            input.Position = 0;

            var output = new MemoryStream();

            var streamKernel = new KernelStreamClient(kernel,
                new StreamReader(input),
                new StreamWriter(output));

            var task = streamKernel.Start();
            await task;

            output.Position = 0;
            var reader = new StreamReader(output, Encoding.UTF8);

            var text = reader.ReadToEnd();
            var events = text.Split(Environment.NewLine)
                .Select(e => JsonConvert.DeserializeObject<StreamKernelEvent>(e));

            events.Should().Contain(e => e.EventType == "NuGetPackageAdded");
        }
    }
}