// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Markdig;
using Markdig.Renderers;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Rendering;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class KernelExtensions
    {
        public static T UseDefaultMagicCommands<T>(this T kernel)
            where T : KernelBase
        {
            kernel.UseLsMagic()
                  .UseHtml()
                  .UseJavaScript()
                  .UseMarkdown()
                  .UseTime();

            return kernel;
        }

        private static T UseHtml<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(new Command("%%html")
            {
                Handler =  CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var htmlContent = submitCode.Code
                                                      .Replace("%%html", "")
                                                      .Trim();
                      
                        context.Publish(new DisplayedValueProduced(
                                           htmlContent,
                                           context.Command,
                                           formattedValues: new[]
                                           {
                                               new FormattedValue("text/html", htmlContent)
                                           }));

                        context.Complete();
                    }
                })
            });

            return kernel;
        }

        private static T UseMarkdown<T>(this T kernel)
            where T : KernelBase
        {
            var pipeline = new MarkdownPipelineBuilder()
                   .UseMathematics()
                   .UseAdvancedExtensions()
                   .Build();

            kernel.AddDirective(new Command("%%markdown")
            {
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var markdown = submitCode.Code
                                                 .Replace("%%markdown", "")
                                                 .Trim();

                        var document = Markdown.Parse(
                            markdown,
                            pipeline);

                        string html;

                        using (var writer = new StringWriter())
                        {
                            var renderer = new HtmlRenderer(writer);
                            pipeline.Setup(renderer);
                            renderer.Render(document);
                            html = writer.ToString();
                        }

                        context.Publish(
                            new DisplayedValueProduced(
                                html,
                                context.Command,
                                new[]
                                {
                                    new FormattedValue("text/html", html)
                                }));

                        context.Complete();
                    }
                })
            });

            return kernel;
        }

        private static T UseJavaScript<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(javascript());

            return kernel;
        }

        private static T UseTime<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(time());

            return kernel;
        }

        private static T UseLsMagic<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(lsmagic());

            kernel.VisitSubkernels(k =>
            {
                if (k is KernelBase kb)
                {
                    kb.AddDirective(lsmagic());
                }
            });

            Formatter<SupportedDirectives>.Register((directives, writer) =>
            {
                PocketView t = div(
                    h6(directives.KernelName),
                    pre(directives.Commands.Select(Describe)));

                t.WriteTo(writer, HtmlEncoder.Default);
            }, "text/html");

            return kernel;

            IHtmlContent Describe(ICommand command)
            {
                return span(command.Name, " ");
            }
        }

        private static Command lsmagic()
        {
            return new Command("%lsmagic")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    var supportedDirectives = new SupportedDirectives(context.CurrentKernel.Name);

                    supportedDirectives.Commands.AddRange(context.CurrentKernel.Directives);

                    context.Publish(new DisplayedValueProduced(supportedDirectives, context.Command));

                    await context.CurrentKernel.VisitSubkernelsAsync(async k =>
                    {
                        if (k.Directives.Any(d => d.Name == "%lsmagic"))
                        {
                            await k.SendAsync(context.Command);
                        }
                    });
                })
            };
        }

        private static Command javascript()
        {
            return new Command("%%javascript")
            {
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var scriptContent = submitCode.Code
                                                      .Replace("%%javascript", "")
                                                      .Trim();

                        string value =
                            script[type: "text/javascript"](
                                    HTML(
                                        scriptContent))
                                .ToString();

                        context.Publish(new DisplayedValueProduced(
                                            scriptContent,
                                            context.Command,
                                            formattedValues: new[]
                                            {
                                                new FormattedValue("text/html",
                                                                   value)
                                            }));

                        context.Complete();
                    }
                })
            };
        }

        private static Command time()
        {
            return new Command("%%time")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var code = submitCode.Code
                                             .Replace("%%time", "")
                                             .Trim();

                        var timer = new Stopwatch();
                        timer.Start();

                        await context.CurrentKernel.SendAsync(
                            new SubmitCode(code, submitCode.TargetKernelName));

                        var elapsed = timer.Elapsed;

                        var formattableString = $"Wall time: {elapsed.TotalMilliseconds}ms";

                        context.Publish(
                            new DisplayedValueProduced(
                                elapsed, 
                                context.Command,
                                new[]
                                {
                                    new FormattedValue(PlainTextFormatter.MimeType, formattableString)
                                }));

                        context.Complete();
                    }
                })
            };
        }
    }
}