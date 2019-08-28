// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Rendering;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class MagicCommandTests
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public MagicCommandTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        [Fact]
        public async Task lsmagic_lists_registered_magic_commands()
        {
            var kernel = new CompositeKernel()
                .UseDefaultMagicCommands()
                .LogEventsToPocketLogger();

            kernel.AddDirective(new Command("%%one"));
            kernel.AddDirective(new Command("%%two"));
            kernel.AddDirective(new Command("%%three"));

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode("%lsmagic"));

            events.Should()
                  .ContainSingle(e => e is ValueProduced)
                  .Which
                  .As<ValueProduced>()
                  .Value
                  .ToDisplayString("text/html")
                  .Should()
                  .ContainAll("%lsmagic", "%%one", "%%three", "%%two");
        }

        [Fact]
        public async Task lsmagic_lists_registered_magic_commands_in_subkernels()
        {
            var subkernel1 = new CSharpKernel();
            subkernel1.AddDirective(new Command("%%from-subkernel-1"));
            var subkernel2 = new FSharpKernel();
            subkernel2.AddDirective(new Command("%%from-subkernel-2"));

            var compositeKernel = new CompositeKernel
                                  {
                                      subkernel1,
                                      subkernel2
                                  }
                                  .UseDefaultMagicCommands()
                                  .LogEventsToPocketLogger();

            compositeKernel.AddDirective(new Command("%%from-compositekernel"));

            using var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("%lsmagic"));

            var valueProduceds = events.OfType<ValueProduced>().ToArray();

            valueProduceds[0].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("%lsmagic",
                                         "%%csharp",
                                         "%%fsharp",
                                         "%%from-compositekernel");

            valueProduceds[1].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("%lsmagic",
                                         "%%from-subkernel-1");
            valueProduceds[2].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("%lsmagic",
                                         "%%from-subkernel-2");
        }

        [Fact]
        public async Task html_emits_string_as_content_within_a_script_element()
        {
            var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var html = "<b>hello!</b>";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%html\n\n{html}"));

            var formatted =
                events
                    .OfType<ValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            Log.Info(events.ToDisplayString());

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Equals(html));
        }

        [Fact]
        public async Task javascript_emits_string_as_content_within_a_script_element()
        {
            var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var scriptContent = "alert('Hello World!');";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%javascript\n{scriptContent}"));

            var formatted =
                events
                    .OfType<ValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            Log.Info(events.ToDisplayString());

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Equals($@"<script type=""text/javascript"">{scriptContent}</script>"));
        }
    }
}
