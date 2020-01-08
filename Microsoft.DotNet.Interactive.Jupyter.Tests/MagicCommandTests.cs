// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Tests;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;

#pragma warning disable 8509 // don't warn on incomplete pattern matches
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
            using var kernel = new CompositeKernel()
                               .UseDefaultMagicCommands()
                               .LogEventsToPocketLogger();

            kernel.AddDirective(new Command("%%one"));
            kernel.AddDirective(new Command("%%two"));
            kernel.AddDirective(new Command("%%three"));

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode("%lsmagic"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced)
                  .Which
                  .As<DisplayedValueProduced>()
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

            using var compositeKernel = new CompositeKernel
                                  {
                                      subkernel1,
                                      subkernel2
                                  }
                                  .UseDefaultMagicCommands()
                                  .LogEventsToPocketLogger();

            compositeKernel.AddDirective(new Command("%%from-compositekernel"));

            using var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("%lsmagic"));

            var valueProduceds = events.OfType<DisplayedValueProduced>().ToArray();

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
            using var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var html = "<b>hello!</b>";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%html\n\n{html}"));

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
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
            using var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var scriptContent = "alert('Hello World!');";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%javascript\n{scriptContent}"));

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            Log.Info(events.ToDisplayString());

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Equals($@"<script type=""text/javascript"">{scriptContent}</script>"));
        }


        [Fact]
        public async Task markdown_renders_markdown_content_as_html()
        {
            using var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var expectedHtml = @"<h1 id=""topic"">Topic!</h1><p>Content</p>";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%markdown\n\n# Topic!\nContent"));
            

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Crunch().Equals(expectedHtml));
        }

        [Fact]
        public async Task time_produces_time_elapsed_to_run_the_code_submission()
        {
            using var kernel = new CompositeKernel
                {
                    new CSharpKernel().UseKernelHelpers()
                }
                .UseDefaultMagicCommands()
                .LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       @"
%%time

using System.Threading.Tasks;
await Task.Delay(500);
display(""done!"");
"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced &&
                                      e.As<DisplayedValueProduced>().Value is TimeSpan)
                  .Which
                  .As<DisplayedValueProduced>()
                  .FormattedValues
                  .Should()
                  .ContainSingle(v =>
                                     v.MimeType == "text/plain" &&
                                     v.Value.ToString().StartsWith("Wall time:") &&
                                     v.Value.ToString().EndsWith("ms"));

            events.OfType<DisplayedValueProduced>()
                  .SelectMany(e => e.FormattedValues)
                  .Should()
                  .Contain(v => v.Value.Equals("done!"));
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task whos_lists_the_names_and_values_of_variables_in_scope(Language language)
        {
            using var baseKernel = language switch
            {
                Language.CSharp => new CSharpKernel().UseWho() as KernelBase,
                Language.FSharp => new FSharpKernel().UseWho(),
            };
            using var kernel = new CompositeKernel
                         {
                             baseKernel
                         }
                         .LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var commands = language switch
            {
                Language.CSharp => new[]
                {
                    "var x = 1;",
                    "x = 2;",
                    "var y = \"hi!\";",
                    "var z = new object[] { x, y };",
                },
                Language.FSharp => new[]
                {
                    "let mutable x = 1",
                    "x <- 2",
                    "let y = \"hi!\"",
                    "let z = [| x :> obj; y :> obj |]",
                },
            };

            foreach (var command in commands)
            {
                await kernel.SendAsync(new SubmitCode(command));
            }

            await kernel.SendAsync(new SubmitCode(@"%whos"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced)
                  .Which
                  .As<DisplayedValueProduced>()
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == "text/html")
                  .Which
                  .Value
                  .As<string>()
                  .Should()
                  .ContainAll(
                      "<td>x</td><td>System.Int32</td><td>2</td>",
                      "<td>y</td><td>System.String</td><td>hi!</td>",
                      "<td>z</td><td>System.Object[]</td><td>[ 2, hi! ]</td>");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task who_lists_the_names_of_variables_in_scope(Language language)
        {
            using var baseKernel = language switch
            {
                Language.CSharp => new CSharpKernel().UseWho() as KernelBase,
                Language.FSharp => new FSharpKernel().UseWho(),
            };
            using var kernel = new CompositeKernel
                         {
                             baseKernel
                         }
                         .LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var commands = language switch
            {
                Language.CSharp => new[]
                {
                    "var x = 1;",
                    "x = 2;",
                    "var y = \"hi!\";",
                    "var z = new object[] { x, y };",
                },
                Language.FSharp => new[]
                {
                    "let mutable x = 1",
                    "x <- 2",
                    "let y = \"hi!\"",
                    "let z = [| x :> obj; y :> obj |]",
                },
            };

            foreach (var command in commands)
            {
                await kernel.SendAsync(new SubmitCode(command));
            }

            await kernel.SendAsync(new SubmitCode(@"%who"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced)
                  .Which
                  .As<DisplayedValueProduced>()
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == "text/html")
                  .Which
                  .Value
                  .As<string>()
                  .Should()
                  .ContainAll("x", "y", "z");
        }

        [Fact]
        public async Task magic_command_parse_errors_are_displayed()
        {
            var command = new Command("%oops")
            {
                new Argument<string>()
            };

            using var kernel = new CSharpKernel();

            kernel.AddDirective(command);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("%oops");

            events.Should()
                  .ContainSingle<ErrorProduced>()
                  .Which
                  .Message
                  .Should()
                  .Be("Required argument missing for command: %oops");
        }

        [Fact]
        public async Task magic_command_parse_errors_prevent_code_submission_from_being_run()
        {
            var command = new Command("%oops")
            {
                new Argument<string>()
            };

            using var kernel = new CSharpKernel();

            kernel.AddDirective(command);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("%oops\n123");

            events.Should().NotContain(e => e is ReturnValueProduced);
        }
    }
}
