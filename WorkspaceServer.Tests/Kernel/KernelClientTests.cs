﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Newtonsoft.Json;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public class KernelClientTests
    {
        [Fact]
        public async Task Kernel_can_be_interacted_using_kernel_client()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            var input = new MemoryStream();
            var writer = new StreamWriter(input, Encoding.UTF8);
            writer.WriteMessage(new SubmitCode(@"var x = 
123;"));
            writer.WriteMessage(new SubmitCode("x"));
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
                             .Select(JsonConvert.DeserializeObject<StreamKernelEvent>);

            events.Should().Contain(e => e.EventType == "ReturnValueProduced");
        }

        [Fact]
        public async Task Kernel_client_surfaces_json_errors()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context => Task.CompletedTask
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
            var events = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(JsonConvert.DeserializeObject<StreamKernelEvent>);

            events.Should().Contain(e => e.EventType == "CommandParseFailure");
        }

        [Fact]
        public async Task Kernel_can_pound_r_nuget_using_kernel_client()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
            };

            var input = new MemoryStream();
            var writer = new StreamWriter(input, Encoding.UTF8);
            writer.WriteMessage(new SubmitCode(@"#r ""nuget:Microsoft.Spark, 0.4.0"""));
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
            var events = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(JsonConvert.DeserializeObject<StreamKernelEvent>).ToList();

            events.Should()
                .Contain(e => e.EventType == nameof(NuGetPackageAdded));

            events.Should()
                .NotContain(e => e.EventType == nameof(CommandNotRecognized));
        }
    }
}